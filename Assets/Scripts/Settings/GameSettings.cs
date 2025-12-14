using UnityEngine;

/// <summary>
/// Static class for managing persistent game settings via PlayerPrefs.
/// </summary>
public static class GameSettings
{
    private const string FRAME_RATE_KEY = "FrameRateLimit";
    private const int DEFAULT_FRAME_RATE = 60;

    /// <summary>
    /// Available frame rate options. -1 represents unlimited.
    /// </summary>
    public static readonly int[] FrameRateOptions = { 60, 120, 240, -1 };
    public static readonly string[] FrameRateLabels = { "60 FPS", "120 FPS", "240 FPS", "Unlimited" };

    /// <summary>
    /// Gets or sets the target frame rate. -1 means unlimited.
    /// </summary>
    public static int TargetFrameRate
    {
        get => PlayerPrefs.GetInt(FRAME_RATE_KEY, DEFAULT_FRAME_RATE);
        set
        {
            PlayerPrefs.SetInt(FRAME_RATE_KEY, value);
            PlayerPrefs.Save();
            ApplyFrameRate(value);
        }
    }

    /// <summary>
    /// Gets the index of the current frame rate in FrameRateOptions array.
    /// </summary>
    public static int CurrentFrameRateIndex
    {
        get
        {
            int current = TargetFrameRate;
            for (int i = 0; i < FrameRateOptions.Length; i++)
            {
                if (FrameRateOptions[i] == current)
                    return i;
            }
            return 0; // Default to 60 FPS if not found
        }
    }

    /// <summary>
    /// Applies the frame rate setting to the application.
    /// </summary>
    public static void ApplyFrameRate(int frameRate)
    {
        QualitySettings.vSyncCount = 0; // Disable VSync to respect targetFrameRate
        Application.targetFrameRate = frameRate;
    }

    /// <summary>
    /// Sets frame rate by index in the FrameRateOptions array.
    /// </summary>
    public static void SetFrameRateByIndex(int index)
    {
        if (index >= 0 && index < FrameRateOptions.Length)
        {
            TargetFrameRate = FrameRateOptions[index];
        }
    }

    /// <summary>
    /// Initializes settings on game startup.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        ApplyFrameRate(TargetFrameRate);
    }
}
