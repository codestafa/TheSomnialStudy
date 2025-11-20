using UnityEngine;

/// <summary>
/// Camera shake effect that can be triggered from Timeline or code
/// Add this to your Main Camera
/// </summary>
public class CameraShake : MonoBehaviour
{
    [Header("Shake Parameters")]
    [SerializeField] private float shakeMagnitude = 0.5f;
    [SerializeField] private float shakeRoughness = 10f;
    [SerializeField] private float fadeFactor = 1f;

    private Vector3 originalPosition;
    private float shakeIntensity = 0f;
    private bool isShaking = false;

    private void Awake()
    {
        originalPosition = transform.localPosition;
    }

    private void LateUpdate()
    {
        if (isShaking && shakeIntensity > 0)
        {
            // Generate random shake offset
            Vector3 shakeOffset = Random.insideUnitSphere * shakeMagnitude * shakeIntensity;
            transform.localPosition = originalPosition + shakeOffset;

            // Fade out the shake over time
            shakeIntensity -= fadeFactor * Time.deltaTime;

            if (shakeIntensity <= 0)
            {
                isShaking = false;
                transform.localPosition = originalPosition;
            }
        }
    }

    /// <summary>
    /// Start shaking with specified duration and intensity
    /// Can be called from Timeline or UnityEvents
    /// </summary>
    public void Shake(float duration, float intensity = 1f)
    {
        shakeIntensity = intensity;
        fadeFactor = intensity / duration;
        isShaking = true;
    }

    /// <summary>
    /// Quick shake (0.5 seconds)
    /// </summary>
    public void ShakeShort()
    {
        Shake(0.5f, 1f);
    }

    /// <summary>
    /// Medium shake (1 second)
    /// </summary>
    public void ShakeMedium()
    {
        Shake(1f, 1f);
    }

    /// <summary>
    /// Long shake (2 seconds)
    /// </summary>
    public void ShakeLong()
    {
        Shake(2f, 1f);
    }

    /// <summary>
    /// Earthquake effect (continuous rumble)
    /// </summary>
    public void StartEarthquake()
    {
        Shake(5f, 1.5f);
    }

    /// <summary>
    /// Explosion shake (strong but quick)
    /// </summary>
    public void ShakeExplosion()
    {
        Shake(0.8f, 2f);
    }

    /// <summary>
    /// Stop shaking immediately
    /// </summary>
    public void StopShake()
    {
        isShaking = false;
        shakeIntensity = 0f;
        transform.localPosition = originalPosition;
    }

    /// <summary>
    /// Set the shake magnitude (how far the camera moves)
    /// </summary>
    public void SetMagnitude(float magnitude)
    {
        shakeMagnitude = magnitude;
    }

    /// <summary>
    /// Set the shake roughness (how jittery it is)
    /// </summary>
    public void SetRoughness(float roughness)
    {
        shakeRoughness = roughness;
    }

    private void OnDisable()
    {
        // Reset position when disabled
        transform.localPosition = originalPosition;
    }
}
