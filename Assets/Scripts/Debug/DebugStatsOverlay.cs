using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Displays FPS, frame time, and memory usage on screen.
/// Only active in Development Builds or Editor.
/// </summary>
public class DebugStatsOverlay : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] private bool showFPS = true;
    [SerializeField] private bool showFrameTime = true;
    [SerializeField] private bool showMemory = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.F3;

    [Header("Appearance")]
    [SerializeField] private Color goodColor = new Color(0.2f, 0.9f, 0.3f, 1f);
    [SerializeField] private Color warningColor = new Color(1f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color badColor = new Color(1f, 0.3f, 0.2f, 1f);
    [SerializeField] private int fontSize = 14;
    [SerializeField] private TextAnchor anchor = TextAnchor.UpperLeft;
    [SerializeField] private Vector2 padding = new Vector2(10, 10);

    [Header("FPS Thresholds")]
    [SerializeField] private float goodFPS = 55f;
    [SerializeField] private float warningFPS = 30f;

    // FPS calculation
    private float deltaTime = 0f;
    private float fps = 0f;
    private float smoothFPS = 0f;
    private const float FPS_SMOOTHING = 0.1f;

    // Frame time tracking
    private float minFrameTime = float.MaxValue;
    private float maxFrameTime = 0f;
    private float frameTimeResetTimer = 0f;
    private const float FRAME_TIME_RESET_INTERVAL = 2f;

    // Memory tracking
    private long allocatedMemory = 0;
    private long reservedMemory = 0;
    private float memoryUpdateTimer = 0f;
    private const float MEMORY_UPDATE_INTERVAL = 0.5f;

    // UI
    private bool isVisible = true;
    private GUIStyle style;
    private GUIStyle shadowStyle;
    private Rect displayRect;

    private void Awake()
    {
#if !DEVELOPMENT_BUILD && !UNITY_EDITOR
        // Disable in release builds
        enabled = false;
        return;
#endif
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        // Toggle visibility
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
        }

        if (!isVisible) return;

        // Calculate FPS
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * FPS_SMOOTHING;
        fps = 1f / Time.unscaledDeltaTime;
        smoothFPS = 1f / deltaTime;

        // Track frame time min/max
        float currentFrameTime = Time.unscaledDeltaTime * 1000f;
        minFrameTime = Mathf.Min(minFrameTime, currentFrameTime);
        maxFrameTime = Mathf.Max(maxFrameTime, currentFrameTime);

        // Reset min/max periodically
        frameTimeResetTimer += Time.unscaledDeltaTime;
        if (frameTimeResetTimer >= FRAME_TIME_RESET_INTERVAL)
        {
            frameTimeResetTimer = 0f;
            minFrameTime = currentFrameTime;
            maxFrameTime = currentFrameTime;
        }

        // Update memory less frequently
        memoryUpdateTimer += Time.unscaledDeltaTime;
        if (memoryUpdateTimer >= MEMORY_UPDATE_INTERVAL)
        {
            memoryUpdateTimer = 0f;
            allocatedMemory = Profiler.GetTotalAllocatedMemoryLong();
            reservedMemory = Profiler.GetTotalReservedMemoryLong();
        }
#endif
    }

    private void OnGUI()
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        if (!isVisible) return;

        InitStyles();

        float x = padding.x;
        float y = padding.y;

        // Adjust position based on anchor
        if (anchor == TextAnchor.UpperRight || anchor == TextAnchor.MiddleRight || anchor == TextAnchor.LowerRight)
        {
            x = Screen.width - padding.x - 200;
        }
        if (anchor == TextAnchor.LowerLeft || anchor == TextAnchor.LowerCenter || anchor == TextAnchor.LowerRight)
        {
            y = Screen.height - padding.y - 100;
        }

        float lineHeight = fontSize + 4;
        int lineCount = 0;

        // FPS display
        if (showFPS)
        {
            string fpsText = $"FPS: {smoothFPS:F1} ({fps:F0})";
            Color fpsColor = GetFPSColor(smoothFPS);
            DrawTextWithShadow(new Rect(x, y + lineHeight * lineCount, 300, lineHeight), fpsText, fpsColor);
            lineCount++;
        }

        // Frame time display
        if (showFrameTime)
        {
            float frameTimeMs = deltaTime * 1000f;
            string frameTimeText = $"Frame: {frameTimeMs:F2}ms (min:{minFrameTime:F1} max:{maxFrameTime:F1})";
            Color frameColor = GetFPSColor(smoothFPS);
            DrawTextWithShadow(new Rect(x, y + lineHeight * lineCount, 300, lineHeight), frameTimeText, frameColor);
            lineCount++;
        }

        // Memory display
        if (showMemory)
        {
            float allocMB = allocatedMemory / (1024f * 1024f);
            float reservedMB = reservedMemory / (1024f * 1024f);
            string memoryText = $"Memory: {allocMB:F1}MB / {reservedMB:F1}MB";
            DrawTextWithShadow(new Rect(x, y + lineHeight * lineCount, 300, lineHeight), memoryText, goodColor);
            lineCount++;
        }

        // Toggle hint
        string hintText = $"[{toggleKey}] Toggle Stats";
        style.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
        GUI.Label(new Rect(x, y + lineHeight * lineCount + 5, 300, lineHeight), hintText, style);
#endif
    }

    private void InitStyles()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };
        }

        if (shadowStyle == null)
        {
            shadowStyle = new GUIStyle(style)
            {
                normal = { textColor = new Color(0, 0, 0, 0.8f) }
            };
        }
    }

    private void DrawTextWithShadow(Rect rect, string text, Color color)
    {
        // Draw shadow
        Rect shadowRect = new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height);
        GUI.Label(shadowRect, text, shadowStyle);

        // Draw text
        style.normal.textColor = color;
        GUI.Label(rect, text, style);
    }

    private Color GetFPSColor(float currentFPS)
    {
        if (currentFPS >= goodFPS)
            return goodColor;
        if (currentFPS >= warningFPS)
            return Color.Lerp(warningColor, goodColor, (currentFPS - warningFPS) / (goodFPS - warningFPS));
        return Color.Lerp(badColor, warningColor, currentFPS / warningFPS);
    }

    /// <summary>
    /// Creates a DebugStatsOverlay if one doesn't exist.
    /// Call this from a bootstrap script or first scene.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        if (FindObjectOfType<DebugStatsOverlay>() == null)
        {
            GameObject go = new GameObject("[Debug Stats Overlay]");
            go.AddComponent<DebugStatsOverlay>();
        }
#endif
    }
}
