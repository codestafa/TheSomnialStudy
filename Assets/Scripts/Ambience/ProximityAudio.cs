using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class ProximityAudio : MonoBehaviour
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
    [Tooltip("Starting volume level when scene begins (if not silent).")]
    public float startVolume = 0.3f;

    [Tooltip("How smoothly the volume transitions adjust.")]
    public float fadeSmoothTime = 0.5f;

    [Header("⚙️ Options")]
    [Tooltip("If enabled, the audio starts completely silent until player enters trigger.")]
    public bool startSilent = true;

    private SphereCollider triggerZone;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        triggerZone = GetComponent<SphereCollider>();

        if (triggerZone == null)
        {
            Debug.LogWarning($"{name}: No SphereCollider found! Adding one automatically.");
            triggerZone = gameObject.AddComponent<SphereCollider>();
            triggerZone.isTrigger = true;
            triggerZone.radius = 10f;
        }

        audioSource.loop = true;
        audioSource.playOnAwake = false;

        // 🔧 Initialize correctly
        audioSource.Stop();
        audioSource.volume = startSilent ? 0f : startVolume;
    }

    IEnumerator Start()
    {
        // Find player and give Unity one frame before we start calculating distance
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            Debug.LogError($"{name}: No object tagged 'Player' found in the scene!");
            enabled = false;
            yield break;
        }

        // Play the sound at startup volume
        audioSource.Play();
        audioSource.volume = startSilent ? 0f : startVolume;

        // Wait a moment before distance fades kick in
        yield return new WaitForSeconds(0.25f);
        initialized = true;
    }

    void Update()
    {
        if (!initialized || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        float maxDistance = triggerZone.radius;

        // Calculate normalized proximity (1 = close, 0 = far)
        float normalized = Mathf.Clamp01(1f - (distance / maxDistance));

        // Calculate the desired volume (minVolume to 1)
        float targetVolume = Mathf.Lerp(minVolume, 1f, normalized);

        // Smoothly adjust to target
        audioSource.volume = Mathf.SmoothDamp(audioSource.volume, targetVolume, ref fadeVelocity, fadeSmoothTime);

        // Handle stopping when too far and muted
        if (distance > maxDistance + 1f && audioSource.isPlaying)
        {
            if (audioSource.volume <= 0.01f)
                audioSource.Stop();
        }
        else if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere != null && sphere.isTrigger)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, sphere.radius);
        }
    }
#endif
}
