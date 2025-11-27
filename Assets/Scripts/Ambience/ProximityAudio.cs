using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class ProximityAudioEnhanced : MonoBehaviour
{
    private AudioSource audioSource;
    private Transform player;
    private float fadeVelocity;
    private bool initialized = false;

    [Header("🔊 Volume Settings")]
    [Range(0f, 1f)]
    [Tooltip("Lowest possible volume when player is far.")]
    public float minVolume = 0f;

    [Range(0f, 1f)]
    [Tooltip("Maximum volume when player is closest.")]
    public float maxVolume = 1f;

    [Range(0f, 1f)]
    [Tooltip("Starting volume level when scene begins (if not silent).")]
    public float startVolume = 0.3f;

    [Tooltip("How smoothly the volume transitions adjust.")]
    public float fadeSmoothTime = 0.5f;

    [Header("⚙ Detection Options")]
    [Tooltip("If enabled, the audio starts completely silent until player enters trigger.")]
    public bool startSilent = true;

    [Tooltip("Use raycast to check if player has line of sight to audio source.")]
    public bool useRaycastDetection = false;

    [Tooltip("Layers that block audio (walls, terrain, etc). Only used if useRaycastDetection is true.")]
    public LayerMask occlusionLayers;

    [Range(0f, 1f)]
    [Tooltip("Volume multiplier when raycast is blocked (occluded).")]
    public float occludedVolumeMultiplier = 0.3f;

    [Tooltip("Maximum raycast distance for line-of-sight checks.")]
    public float raycastDistance = 100f;

    [Header("📍 Detection Mode")]
    [Tooltip("How to detect proximity to the player.")]
    public DetectionMode detectionMode = DetectionMode.SphereCollider;

    [Tooltip("For terrain/large objects: check player distance to nearest point on bounds.")]
    public bool useClosestPoint = true;

    [Tooltip("Manual detection radius (only used if detectionMode is ManualRadius).")]
    public float manualRadius = 10f;

    [Header("🎵 Audio Behavior")]
    [Tooltip("Fade out audio completely when occluded instead of just reducing volume.")]
    public bool fadeOutWhenOccluded = false;

    [Tooltip("Add low-pass filter effect when occluded (muffled sound).")]
    public bool useLowPassWhenOccluded = false;

    [Range(500f, 22000f)]
    [Tooltip("Low-pass filter cutoff frequency when occluded.")]
    public float lowPassCutoff = 1000f;

    [Header("🔧 Advanced Settings")]
    [Tooltip("How often to update distance calculations (in seconds). Lower = more accurate but more CPU intensive.")]
    public float updateInterval = 0.1f;

    [Tooltip("Enable debug visualization and logging.")]
    public bool debugMode = false;

    private SphereCollider triggerZone;
    private AudioLowPassFilter lowPassFilter;
    private float nextUpdateTime;
    private bool isOccluded = false;
    private Vector3 closestPointToPlayer;

    public enum DetectionMode
    {
        SphereCollider,
        ManualRadius,
        BoundsClosestPoint
    }

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        // Setup based on detection mode
        if (detectionMode == DetectionMode.SphereCollider)
        {
            triggerZone = GetComponent<SphereCollider>();
            if (triggerZone == null)
            {
                LogDebug($"No SphereCollider found! Adding one automatically.", "#FFA500");
                triggerZone = gameObject.AddComponent<SphereCollider>();
                triggerZone.isTrigger = true;
                triggerZone.radius = 10f;
            }
        }

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // Full 3D audio

        // Add low-pass filter if needed
        if (useLowPassWhenOccluded)
        {
            lowPassFilter = GetComponent<AudioLowPassFilter>();
            if (lowPassFilter == null)
            {
                lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
            }
            lowPassFilter.cutoffFrequency = 22000f; // Start with no filtering
        }

        // Initialize correctly
        audioSource.Stop();
        audioSource.volume = startSilent ? 0f : startVolume;

        LogDebug("=== ProximityAudio Initialized ===", "#00FFFF");
        LogDebug($"Detection Mode: {detectionMode}", "#CCCCCC");
        LogDebug($"Raycast Detection: {useRaycastDetection}", "#CCCCCC");
        LogDebug($"Use Closest Point: {useClosestPoint}", "#CCCCCC");
    }

    IEnumerator Start()
    {
        // Find player and give Unity one frame before we start calculating distance
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            LogDebug("No object tagged 'Player' found in the scene!", "#FF0000");
            enabled = false;
            yield break;
        }

        // Play the sound at startup volume
        audioSource.Play();
        audioSource.volume = startSilent ? 0f : startVolume;

        // Wait a moment before distance fades kick in
        yield return new WaitForSeconds(0.25f);
        initialized = true;

        LogDebug("Proximity audio system active!", "#00FF00");
    }

    void Update()
    {
        if (!initialized || player == null) return;

        // Throttle updates for performance
        if (Time.time < nextUpdateTime) return;
        nextUpdateTime = Time.time + updateInterval;

        // Calculate distance and proximity
        float distance = GetDistanceToPlayer();
        float maxDistance = GetMaxDistance();

        // Check for occlusion via raycast
        bool previouslyOccluded = isOccluded;
        if (useRaycastDetection)
        {
            CheckOcclusion();
        }
        else
        {
            isOccluded = false;
        }

        // Calculate normalized proximity (1 = close, 0 = far)
        float normalized = Mathf.Clamp01(1f - (distance / maxDistance));

        // Calculate the desired volume (minVolume to maxVolume)
        float targetVolume = Mathf.Lerp(minVolume, maxVolume, normalized);

        // Apply occlusion modifier
        if (isOccluded)
        {
            if (fadeOutWhenOccluded)
            {
                targetVolume = 0f;
            }
            else
            {
                targetVolume *= occludedVolumeMultiplier;
            }

            // Apply low-pass filter when occluded
            if (useLowPassWhenOccluded && lowPassFilter != null)
            {
                lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassFilter.cutoffFrequency, lowPassCutoff, Time.deltaTime * 5f);
            }
        }
        else
        {
            // Remove low-pass filter when not occluded
            if (useLowPassWhenOccluded && lowPassFilter != null)
            {
                lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassFilter.cutoffFrequency, 22000f, Time.deltaTime * 5f);
            }
        }

        // Smoothly adjust to target
        audioSource.volume = Mathf.SmoothDamp(audioSource.volume, targetVolume, ref fadeVelocity, fadeSmoothTime);

        // Debug logging
        if (debugMode && (Time.frameCount % 60 == 0 || previouslyOccluded != isOccluded))
        {
            LogDebug($"Distance: {distance:F1}m | Proximity: {normalized:F2} | Volume: {audioSource.volume:F2} | Occluded: {isOccluded}", "#FFFF00");
        }

        // Handle stopping when too far and muted
        if (distance > maxDistance + 1f && audioSource.isPlaying)
        {
            if (audioSource.volume <= 0.01f)
            {
                audioSource.Stop();
                LogDebug("Audio stopped (too far)", "#FFA500");
            }
        }
        else if (!audioSource.isPlaying)
        {
            audioSource.Play();
            LogDebug("Audio resumed (player in range)", "#00FF00");
        }
    }

    /// <summary>
    /// Gets the distance from player to this audio source based on detection mode
    /// </summary>
    private float GetDistanceToPlayer()
    {
        if (player == null) return float.MaxValue;

        Vector3 checkPoint = transform.position;

        // For terrain or large objects, use closest point on bounds
        if (useClosestPoint && detectionMode == DetectionMode.BoundsClosestPoint)
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                closestPointToPlayer = col.ClosestPoint(player.position);
                checkPoint = closestPointToPlayer;
            }
            else
            {
                // Fallback: try to get bounds from renderer
                Renderer rend = GetComponent<Renderer>();
                if (rend != null)
                {
                    closestPointToPlayer = rend.bounds.ClosestPoint(player.position);
                    checkPoint = closestPointToPlayer;
                }
            }
        }

        return Vector3.Distance(checkPoint, player.position);
    }

    /// <summary>
    /// Gets the maximum detection distance based on detection mode
    /// </summary>
    private float GetMaxDistance()
    {
        switch (detectionMode)
        {
            case DetectionMode.SphereCollider:
                return triggerZone != null ? triggerZone.radius : 10f;

            case DetectionMode.ManualRadius:
                return manualRadius;

            case DetectionMode.BoundsClosestPoint:
                return manualRadius; // Use manual radius as max distance for bounds mode

            default:
                return 10f;
        }
    }

    /// <summary>
    /// Checks if there's a clear line of sight between audio source and player
    /// </summary>
    private void CheckOcclusion()
    {
        if (player == null) return;

        Vector3 sourcePos = useClosestPoint && detectionMode == DetectionMode.BoundsClosestPoint
            ? closestPointToPlayer
            : transform.position;

        Vector3 directionToPlayer = player.position - sourcePos;
        float distanceToPlayer = directionToPlayer.magnitude;

        // Clamp raycast distance
        float actualRaycastDistance = Mathf.Min(distanceToPlayer, raycastDistance);

        if (Physics.Raycast(sourcePos, directionToPlayer.normalized, actualRaycastDistance, occlusionLayers))
        {
            if (!isOccluded)
            {
                LogDebug("Audio OCCLUDED - obstacle detected", "#FF0000");
            }
            isOccluded = true;
        }
        else
        {
            if (isOccluded)
            {
                LogDebug("Audio CLEAR - line of sight restored", "#00FF00");
            }
            isOccluded = false;
        }
    }

    /// <summary>
    /// Debug logging helper
    /// </summary>
    private void LogDebug(string message, string hexColor = "#FFFFFF")
    {
        if (debugMode)
            Debug.Log($"<color={hexColor}>[ProximityAudio:{name}] {message}</color>");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Draw detection sphere
        float radius = GetMaxDistance();
        Vector3 center = transform.position;

        // Draw main detection range
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawWireSphere(center, radius);

        // Draw filled sphere at center
        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        Gizmos.DrawSphere(center, 0.3f);

        if (!Application.isPlaying) return;

        // Draw raycast line to player
        if (useRaycastDetection && player != null)
        {
            Vector3 sourcePos = useClosestPoint && detectionMode == DetectionMode.BoundsClosestPoint
                ? closestPointToPlayer
                : transform.position;

            Gizmos.color = isOccluded ? Color.red : Color.green;
            Gizmos.DrawLine(sourcePos, player.position);

            // Draw raycast distance limit
            Vector3 dirToPlayer = (player.position - sourcePos).normalized;
            Vector3 raycastEnd = sourcePos + dirToPlayer * Mathf.Min(raycastDistance, Vector3.Distance(sourcePos, player.position));

            Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
            Gizmos.DrawWireSphere(raycastEnd, 0.2f);
        }

        // Draw closest point when using bounds mode
        if (useClosestPoint && detectionMode == DetectionMode.BoundsClosestPoint && player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(closestPointToPlayer, 0.5f);
            Gizmos.DrawLine(closestPointToPlayer, player.position);
        }

        // Draw player position
        if (player != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(player.position, 0.3f);
        }
    }

    void OnDrawGizmos()
    {
        if (!debugMode) return;

        // Show audio source icon even when not selected
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
#endif
}