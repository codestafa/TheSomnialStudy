using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Receives signals from Timeline to trigger camera shakes
/// Add this to your Main Camera along with CameraShake
/// Use with Timeline Signal Tracks
/// </summary>
public class CameraShakeSignalReceiver : MonoBehaviour, INotificationReceiver
{
    [Header("References")]
    [SerializeField] private CameraShake cameraShake;

    [Header("Default Shake Settings")]
    [SerializeField] private float defaultDuration = 0.5f;
    [SerializeField] private float defaultIntensity = 1f;

    private void Awake()
    {
        if (cameraShake == null)
        {
            cameraShake = GetComponent<CameraShake>();
        }
    }

    public void OnNotify(Playable origin, INotification notification, object context)
    {
        // Handle Timeline signals
        if (notification is SignalEmitter)
        {
            TriggerShake();
        }
    }

    public void TriggerShake()
    {
        if (cameraShake != null)
        {
            cameraShake.Shake(defaultDuration, defaultIntensity);
        }
    }
}
