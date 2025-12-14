using UnityEngine;

/// <summary>
/// Optional component to override frame rate for specific scenes.
/// For global settings, use GameSettings which persists via PlayerPrefs.
/// </summary>
public class FrameRateCap : MonoBehaviour
{
    [Tooltip("If true, overrides the user's saved frame rate setting for this scene.")]
    [SerializeField] private bool overrideUserSetting = false;

    [Tooltip("Frame rate to use when overriding. -1 for unlimited.")]
    [SerializeField] private int targetFrameRate = 60;

    private void Awake()
    {
        if (overrideUserSetting)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;
        }
        // Otherwise, GameSettings handles the frame rate via RuntimeInitializeOnLoadMethod
    }
}
