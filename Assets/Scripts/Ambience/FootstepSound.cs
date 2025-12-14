using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FootstepSoundImproved : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] private LayerMask floorLayer;
    [SerializeField] private bool debugMode = true;

    [Header("Footstep Distance Settings")]
    [Range(0.3f, 2f)][SerializeField] private float walkStepLength = 1.2f;
    [Range(0.3f, 2f)][SerializeField] private float runStepLength = 0.7f;

    [Header("Peak Detection")]
    [Range(2, 20)][SerializeField] private int peakPointCount = 8;
    [Range(0.01f, 0.5f)][SerializeField] private float transientThreshold = 0.15f;

    [Header("Audio Settings")]
    [Range(0f, 1f)][SerializeField] private float minVolume = 0.3f;
    [Range(0f, 1f)][SerializeField] private float maxVolume = 0.5f;
    [Range(0.8f, 1.2f)][SerializeField] private float minPitch = 0.93f;
    [Range(0.8f, 1.2f)][SerializeField] private float maxPitch = 1.07f;
    [SerializeField] private float footSeparation = 0.15f;

    [Header("Surface Sounds")]
    [SerializeField] private TextureSound[] textureSounds;
    [SerializeField] private TextureSound defaultSound; // Fallback when no texture match

    [Header("Raycast Settings")]
    [SerializeField] private float raycastDistance = 1.5f;
    [SerializeField] private float raycastOriginOffset = 0.1f;

    private CharacterController controller;
    private PlayerMovement playerMovement;
    private AudioSource audioSource; // Object pooled audio source

    private bool leftFoot = true;
    private float startTime;
    private readonly Dictionary<AudioClip, float[]> cachedPeaks = new();
    private readonly Dictionary<Texture, TextureSound> textureCache = new(); // O(1) lookup

    private Vector3 lastPosition;
    private float smoothedSpeed;
    private float distanceAccumulator;
    private const float SPEED_SMOOTH_FACTOR = 0.15f;
    private const float MIN_MOVEMENT_SPEED = 0.2f;

    // Terrain cache to avoid repeated GetComponent calls
    private readonly Dictionary<Collider, Terrain> terrainCache = new();
    private readonly Dictionary<Collider, Renderer> rendererCache = new();

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();

        // Create a dedicated audio source for better control
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.playOnAwake = false;

        if (!playerMovement)
            LogDebug("[WARNING] PlayerMovement not found! Using fallback speeds.", "#FFA500");

        // Validate floor layer
        if (floorLayer.value == 0)
            LogDebug("[WARNING] FloorLayer is set to Nothing! No footsteps will play.", "#FF0000");
    }

    private void Start()
    {
        startTime = Time.time;
        lastPosition = transform.position;

        LogDebug("=== FOOTSTEP SYSTEM INITIALIZING ===", "#00FFFF");
        LogDebug($"Character Controller: {(controller != null ? "✓" : "✗")}", controller != null ? "#00FF00" : "#FF0000");
        LogDebug($"Player Movement: {(playerMovement != null ? "✓" : "✗")}", playerMovement != null ? "#00FF00" : "#FFA500");
        LogDebug($"Floor Layer: {GetLayerNames(floorLayer)}", floorLayer.value != 0 ? "#00FF00" : "#FF0000");
        LogDebug($"Texture Sounds Array: {textureSounds?.Length ?? 0} entries", "#CCCCCC");
        LogDebug($"Default Sound: {(defaultSound != null ? "SET" : "NOT SET")}", defaultSound != null ? "#00FF00" : "#FFA500");

        BuildTextureCache();
        CacheAllTransientPeaks();

        LogDebug($"=== INITIALIZATION COMPLETE ===", "#00FFFF");

        // Start ground checking AFTER initialization (moved from OnEnable to avoid freeze during scene preload)
        StartCoroutine(CheckGround());
    }

    private void OnEnable()
    {
        // Coroutine moved to Start() to avoid running during scene preload
    }

    /// <summary>
    /// Build a dictionary for O(1) texture lookups instead of O(n) array iteration
    /// </summary>
    private void BuildTextureCache()
    {
        textureCache.Clear();
        LogDebug("=== Building Texture Cache ===", "#00FFFF");

        foreach (var ts in textureSounds)
        {
            if (ts.Albedo != null && !textureCache.ContainsKey(ts.Albedo))
            {
                textureCache[ts.Albedo] = ts;
                LogDebug($"  + Added texture: {ts.Albedo.name} ({ts.Clips?.Length ?? 0} clips)", "#00FF00");
            }
            else if (ts.Albedo == null)
            {
                LogDebug($"  ✗ TextureSound has NULL Albedo!", "#FF0000");
            }
        }

        LogDebug($"=== Texture cache built: {textureCache.Count} textures ===", "#00FFFF");

        if (textureCache.Count == 0)
        {
            LogDebug("⚠⚠⚠ WARNING: Texture cache is EMPTY! No sounds will play unless default sound is set! ⚠⚠⚠", "#FF0000");
        }
    }

    /// <summary>
    /// Pre-cache all transient peaks at startup
    /// </summary>
    private void CacheAllTransientPeaks()
    {
        foreach (var ts in textureSounds)
        {
            if (ts.Clips == null) continue;
            foreach (var clip in ts.Clips)
            {
                if (clip) CacheTransientPeaks(clip);
            }
        }

        // Cache default sound clips too
        if (defaultSound?.Clips != null)
        {
            foreach (var clip in defaultSound.Clips)
            {
                if (clip) CacheTransientPeaks(clip);
            }
        }
    }

    private IEnumerator CheckGround()
    {
        LogDebug("=== CheckGround coroutine started ===", "#00FFFF");

        while (true)
        {
            UpdateMovementTracking();

            if (ShouldPlayFootstep())
            {
                LogDebug($">>> FOOTSTEP TRIGGERED! Distance: {distanceAccumulator:F2}m", "#00FF00");
                PlayFootstep();
            }

            yield return null;
        }
    }

    /// <summary>
    /// Updates position, speed, and distance accumulation
    /// </summary>
    private void UpdateMovementTracking()
    {
        Vector3 currentPos = transform.position;
        float frameDistance = Vector3.Distance(currentPos, lastPosition);
        lastPosition = currentPos;

        float currentSpeed = frameDistance / Time.deltaTime;
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, currentSpeed, SPEED_SMOOTH_FACTOR);

        if (controller.isGrounded && smoothedSpeed > MIN_MOVEMENT_SPEED)
        {
            distanceAccumulator += frameDistance;

            // Debug every 60 frames (roughly once per second at 60fps)
            if (Time.frameCount % 60 == 0)
            {
                LogDebug($"Moving: Speed={smoothedSpeed:F2} m/s | Distance={distanceAccumulator:F2}m | Grounded={controller.isGrounded}", "#FFFF00");
            }
        }
        else
        {
            // Log when conditions aren't met
            if (Time.frameCount % 120 == 0) // Less frequent logging when not moving
            {
                string reason = !controller.isGrounded ? "NOT GROUNDED" : "TOO SLOW";
                LogDebug($"Not accumulating distance: {reason} | Speed={smoothedSpeed:F2} m/s | Grounded={controller.isGrounded}", "#FFA500");
            }
            distanceAccumulator = 0f;
        }
    }

    /// <summary>
    /// Checks if enough distance has been traveled to trigger a footstep
    /// </summary>
    private bool ShouldPlayFootstep()
    {
        if (!controller.isGrounded || smoothedSpeed <= MIN_MOVEMENT_SPEED)
            return false;

        float currentStepLength = GetCurrentStepLength();
        bool shouldPlay = distanceAccumulator >= currentStepLength;

        if (shouldPlay)
        {
            LogDebug($"Footstep condition MET: {distanceAccumulator:F2}m >= {currentStepLength:F2}m", "#00FF00");
        }

        return shouldPlay;
    }

    /// <summary>
    /// Interpolates step length based on current movement speed
    /// </summary>
    private float GetCurrentStepLength()
    {
        float walkSpeed = playerMovement ? playerMovement.GetMovementSpeed() : 3f;
        float runSpeed = playerMovement ? walkSpeed * playerMovement.GetRunMultiplier() : 6f;
        float t = Mathf.InverseLerp(walkSpeed, runSpeed, smoothedSpeed);
        return Mathf.Lerp(walkStepLength, runStepLength, t);
    }

    /// <summary>
    /// Main footstep logic - raycast and play sound
    /// </summary>
    private void PlayFootstep()
    {
        distanceAccumulator = 0f;

        Vector3 origin = transform.position - new Vector3(0, controller.height * 0.5f - controller.radius - raycastOriginOffset, 0);

        LogDebug($"=== RAYCAST FROM: {origin} DOWN {raycastDistance}m ===", "#FFFF00");

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastDistance, floorLayer))
        {
            LogDebug($"✓ Raycast HIT: {hit.collider.name} at distance {hit.distance:F2}m", "#00FF00");
            LogRaycastHit(hit);

            TextureSound soundToPlay = GetTextureSoundForHit(hit);
            if (soundToPlay != null)
            {
                LogDebug($"✓ Sound found for surface, playing...", "#00FF00");
                PlaySoundAtPosition(soundToPlay, hit.point);
            }
            else if (defaultSound != null)
            {
                LogDebug($"⚠ No texture match found, using default sound", "#FFFF00");
                PlaySoundAtPosition(defaultSound, hit.point);
            }
            else
            {
                LogDebug($"✗ No sound found for surface and no default set!", "#FF0000");
            }
        }
        else
        {
            LogDebug($"✗ Raycast MISSED - No ground detected within {raycastDistance}m on layers: {GetLayerNames(floorLayer)}", "#FF0000");
        }

        leftFoot = !leftFoot;
    }

    /// <summary>
    /// Determines which TextureSound to use based on the hit surface
    /// </summary>
    private TextureSound GetTextureSoundForHit(RaycastHit hit)
    {
        LogDebug($"  → Checking surface type for: {hit.collider.name}", "#CCCCCC");

        // Try terrain first
        Terrain terrain = GetCachedTerrain(hit.collider);
        if (terrain != null)
        {
            LogDebug($"  → Object is a TERRAIN", "#CCCCCC");
            Texture tex = GetDominantTerrainTexture(terrain, hit.point);
            if (tex != null)
            {
                LogDebug($"  → Terrain texture: {tex.name}", "#CCCCCC");
                if (textureCache.TryGetValue(tex, out TextureSound ts))
                {
                    LogDebug($"  ✓ Terrain texture '{tex.name}' MATCHED in cache! ({ts.Clips?.Length ?? 0} clips)", "#00FF00");
                    return ts;
                }
                else
                {
                    LogDebug($"  ✗ Terrain texture '{tex.name}' NOT in cache", "#FFA500");
                }
            }
            else
            {
                LogDebug($"  ✗ Could not get terrain texture", "#FF0000");
            }
        }

        // Try renderer
        Renderer rend = GetCachedRenderer(hit.collider);
        if (rend != null)
        {
            LogDebug($"  → Object has RENDERER", "#CCCCCC");
            Texture tex = rend.sharedMaterial?.GetTexture("_MainTex");
            if (tex != null)
            {
                LogDebug($"  → Renderer texture: {tex.name}", "#CCCCCC");
                if (textureCache.TryGetValue(tex, out TextureSound ts))
                {
                    LogDebug($"  ✓ Renderer texture '{tex.name}' MATCHED in cache! ({ts.Clips?.Length ?? 0} clips)", "#00FF00");
                    return ts;
                }
                else
                {
                    LogDebug($"  ✗ Renderer texture '{tex.name}' NOT in cache", "#FFA500");
                }
            }
            else
            {
                LogDebug($"  ✗ Renderer has no _MainTex", "#FFA500");
            }
        }
        else
        {
            LogDebug($"  → Object has NO terrain and NO renderer", "#FFA500");
        }

        return null;
    }

    /// <summary>
    /// Gets cached Terrain component or caches it if not present
    /// </summary>
    private Terrain GetCachedTerrain(Collider collider)
    {
        if (!terrainCache.TryGetValue(collider, out Terrain terrain))
        {
            terrain = collider.GetComponent<Terrain>();
            terrainCache[collider] = terrain;
        }
        return terrain;
    }

    /// <summary>
    /// Gets cached Renderer component or caches it if not present
    /// </summary>
    private Renderer GetCachedRenderer(Collider collider)
    {
        if (!rendererCache.TryGetValue(collider, out Renderer renderer))
        {
            renderer = collider.GetComponent<Renderer>();
            rendererCache[collider] = renderer;
        }
        return renderer;
    }

    /// <summary>
    /// Samples terrain to find the dominant texture at a world position
    /// </summary>
    private Texture GetDominantTerrainTexture(Terrain terrain, Vector3 worldPos)
    {
        TerrainData data = terrain.terrainData;
        Vector3 terrainPos = worldPos - terrain.transform.position;

        // Normalize to terrain space
        Vector3 normalizedPos = new Vector3(
            Mathf.Clamp01(terrainPos.x / data.size.x),
            0,
            Mathf.Clamp01(terrainPos.z / data.size.z)
        );

        int x = Mathf.FloorToInt(normalizedPos.x * (data.alphamapWidth - 1));
        int z = Mathf.FloorToInt(normalizedPos.z * (data.alphamapHeight - 1));

        float[,,] alpha = data.GetAlphamaps(x, z, 1, 1);

        int dominant = 0;
        float maxStrength = alpha[0, 0, 0];

        for (int i = 1; i < alpha.GetLength(2); i++)
        {
            if (alpha[0, 0, i] > maxStrength)
            {
                maxStrength = alpha[0, 0, i];
                dominant = i;
            }
        }

        if (dominant < data.terrainLayers.Length)
            return data.terrainLayers[dominant].diffuseTexture;

        return null;
    }

    /// <summary>
    /// Plays the audio clip with randomization at the foot position
    /// </summary>
    private void PlaySoundAtPosition(TextureSound ts, Vector3 footPosition)
    {
        if (ts.Clips == null || ts.Clips.Length == 0)
        {
            LogDebug($"  ✗ TextureSound has NO CLIPS!", "#FF0000");
            return;
        }

        AudioClip clip = ts.Clips[Random.Range(0, ts.Clips.Length)];
        if (clip == null)
        {
            LogDebug($"  ✗ Selected clip is NULL!", "#FF0000");
            return;
        }

        LogDebug($"  → Selected clip: {clip.name} (length: {clip.length:F2}s)", "#CCCCCC");

        float[] peaks = cachedPeaks.TryGetValue(clip, out float[] arr) ? arr : new float[] { 0f };
        float startTime = peaks.Length > 0 ? peaks[Random.Range(0, peaks.Length)] : 0f;

        float randomPitch = Random.Range(minPitch, maxPitch);
        float randomVolume = Random.Range(minVolume, maxVolume);
        float randomPan = leftFoot ? Random.Range(-0.1f, -0.05f) : Random.Range(0.05f, 0.1f);

        // Calculate foot position with slight offset
        Vector3 soundPos = footPosition + new Vector3(
            leftFoot ? -footSeparation : footSeparation,
            0.05f,
            0f
        );

        LogDebug($"  ♫ PLAYING SOUND: {clip.name}", "#00FFFF");
        LogDebug($"    Volume: {randomVolume:F2} | Pitch: {randomPitch:F2} | Position: {soundPos} | Pan: {randomPan:F2}", "#00FFFF");

        // Use PlayClipAtPoint for spatial audio
        AudioSource.PlayClipAtPoint(clip, soundPos, randomVolume);
    }

    /// <summary>
    /// Caches transient peaks for an audio clip
    /// </summary>
    private void CacheTransientPeaks(AudioClip clip)
    {
        if (!clip || cachedPeaks.ContainsKey(clip)) return;

        int sampleCount = clip.samples * clip.channels;
        float[] samples = new float[sampleCount];
        clip.GetData(samples, 0);

        List<float> peaks = new List<float>();
        int windowSize = Mathf.Max(1, sampleCount / peakPointCount);

        for (int i = 0; i < sampleCount - windowSize; i += windowSize)
        {
            float currentEnergy = 0f;
            float nextEnergy = 0f;

            for (int j = 0; j < windowSize; j++)
            {
                currentEnergy += Mathf.Abs(samples[i + j]);
                if (i + windowSize + j < sampleCount)
                    nextEnergy += Mathf.Abs(samples[i + windowSize + j]);
            }

            if (nextEnergy > currentEnergy * (1f + transientThreshold))
            {
                float peakTime = (float)(i + windowSize) / (clip.frequency * clip.channels);
                peaks.Add(peakTime);
            }
        }

        cachedPeaks[clip] = peaks.Count > 0 ? peaks.ToArray() : new float[] { 0f };
        LogDebug($"Cached {peaks.Count} peaks for clip: {clip.name}");
    }

    /// <summary>
    /// Debug logging helper
    /// </summary>
    private void LogDebug(string message, string hexColor = "#FFFFFF")
    {
        if (debugMode)
            Debug.Log($"<color={hexColor}>[FootstepSound] {message}</color>");
    }

    /// <summary>
    /// Gets a comma-separated list of layer names from a LayerMask
    /// </summary>
    private string GetLayerNames(LayerMask mask)
    {
        List<string> layerNames = new List<string>();
        for (int i = 0; i < 32; i++)
        {
            if ((mask.value & (1 << i)) != 0)
            {
                string layerName = LayerMask.LayerToName(i);
                layerNames.Add(string.IsNullOrEmpty(layerName) ? $"Layer{i}" : layerName);
            }
        }
        return layerNames.Count > 0 ? string.Join(", ", layerNames) : "NONE";
    }

    /// <summary>
    /// Logs detailed raycast hit information
    /// </summary>
    private void LogRaycastHit(RaycastHit hit)
    {
        if (!debugMode) return;

        int hitLayer = hit.collider.gameObject.layer;
        string hitLayerName = LayerMask.LayerToName(hitLayer);
        bool layerAllowed = ((1 << hitLayer) & floorLayer.value) != 0;
        string color = layerAllowed ? "#00FF00" : "#FF0000";

        LogDebug($"Raycast hit <b>{hit.collider.name}</b> | Layer: <b>{hitLayerName} ({hitLayer})</b> | InMask: <b>{layerAllowed}</b>", color);
    }

    private void OnDestroy()
    {
        // Clear caches
        cachedPeaks.Clear();
        textureCache.Clear();
        terrainCache.Clear();
        rendererCache.Clear();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || controller == null) return;

        // Draw raycast origin and direction
        Vector3 origin = transform.position - new Vector3(0, controller.height * 0.5f - controller.radius - raycastOriginOffset, 0);
        Gizmos.color = controller.isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(origin, 0.1f);
        Gizmos.DrawLine(origin, origin + Vector3.down * raycastDistance);

        // Draw step accumulator
        float currentStepLength = GetCurrentStepLength();
        float progress = distanceAccumulator / currentStepLength;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.1f + progress * 0.3f);
    }
#endif
}

[System.Serializable]
public class TextureSound
{
    public Texture Albedo;
    public AudioClip[] Clips;
}