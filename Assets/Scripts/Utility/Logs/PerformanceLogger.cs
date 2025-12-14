using UnityEngine;
using UnityEngine.Profiling;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Logs performance data to a markdown file in development builds.
/// Tracks FPS, frame time, memory, and correlates with Materials and GameObjects.
/// </summary>
public class PerformanceLogger : MonoBehaviour
{
    [Header("Logging Settings")]
    [SerializeField] private float logInterval = 1f;
    [SerializeField] private float heavyObjectScanInterval = 2f;
    [SerializeField] private int maxLogEntries = 500;
    [SerializeField] private bool logLookedAtObject = true;
    [SerializeField] private bool logHeaviestObjects = true;

    [Header("Raycast Settings")]
    [SerializeField] private float raycastDistance = 100f;
    [SerializeField] private LayerMask raycastLayers = ~0;

    [Header("Performance Thresholds")]
    [SerializeField] private float frameSpikeThresholdMs = 33.3f; // Below 30 FPS
    [SerializeField] private int topHeavyObjectCount = 5;

    // File path (single file, overwrites each session)
    private string logFolderPath;
    private string logFilePath;

    // Timing
    private float logTimer = 0f;
    private float heavyScanTimer = 0f;
    private float sessionStartTime;

    // Performance tracking
    private float smoothedFPS = 60f;
    private float smoothedFrameTime = 16.6f;
    private List<FrameData> frameHistory = new List<FrameData>();
    private const int FRAME_HISTORY_SIZE = 120;

    // GameObject and Material tracking
    private GameObject lastLookedAtObject;
    private string lastLookedAtMaterials = "";
    private Camera mainCamera;
    private List<HeavyObjectData> heavyObjects = new List<HeavyObjectData>();
    private Dictionary<string, MaterialPerformanceData> materialPerformanceMap = new Dictionary<string, MaterialPerformanceData>();

    // Log buffer
    private StringBuilder logBuffer = new StringBuilder();
    private int entryCount = 0;
    private bool headerWritten = false;

    private struct FrameData
    {
        public float time;
        public float frameTimeMs;
        public float fps;
        public string lookedAtObject;
        public string materials;
        public long memoryBytes;
    }

    private struct HeavyObjectData
    {
        public GameObject gameObject;
        public string name;
        public string[] materialNames;
        public string shaderNames;
        public int vertexCount;
        public int triangleCount;
        public int materialCount;
        public long memoryEstimate;
        public float renderWeight;
    }

    private class MaterialPerformanceData
    {
        public string materialName;
        public string shaderName;
        public int frameSpikesWhileVisible = 0;
        public int totalFramesVisible = 0;
        public float avgFrameTimeWhileVisible = 0f;
        public float maxFrameTimeWhileVisible = 0f;
    }

    private void Awake()
    {
#if !DEVELOPMENT_BUILD && !UNITY_EDITOR
        enabled = false;
        return;
#endif
        Debug.Log("[PerformanceLogger] === STARTING PERFORMANCE LOGGER ===");
        InitializeLogFile();
        sessionStartTime = Time.realtimeSinceStartup;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        mainCamera = Camera.main;
        ScanForHeavyObjects();
    }

    private void Update()
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        UpdatePerformanceMetrics();
        UpdateLookedAtObject();

        logTimer += Time.unscaledDeltaTime;
        if (logTimer >= logInterval)
        {
            logTimer = 0f;
            LogPerformanceEntry();
        }

        heavyScanTimer += Time.unscaledDeltaTime;
        if (heavyScanTimer >= heavyObjectScanInterval)
        {
            heavyScanTimer = 0f;
            ScanForHeavyObjects();
        }
#endif
    }

    private void OnDestroy()
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        WriteSummaryAndClose();
#endif
    }

    private void OnApplicationQuit()
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        WriteSummaryAndClose();
#endif
    }

    private void InitializeLogFile()
    {
        try
        {
#if UNITY_EDITOR
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            logFolderPath = Path.Combine(projectRoot, "Utility", "Logs");
#else
            logFolderPath = Path.Combine(Application.persistentDataPath, "Logs");
#endif

            Debug.Log($"[PerformanceLogger] Log folder: {logFolderPath}");

            if (!Directory.Exists(logFolderPath))
            {
                Directory.CreateDirectory(logFolderPath);
                Debug.Log($"[PerformanceLogger] Created directory: {logFolderPath}");
            }

            // Single file that overwrites each session
            logFilePath = Path.Combine(logFolderPath, "performance_log.md");

            Debug.Log($"[PerformanceLogger] Writing to: {logFilePath}");

            // Overwrite the log file
            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[PerformanceLogger] Failed to initialize: {e.Message}");
            enabled = false;
            return;
        }

        // Write header
        logBuffer.AppendLine("# Performance Log");
        logBuffer.AppendLine();
        logBuffer.AppendLine($"**Session Start:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        logBuffer.AppendLine($"**Unity Version:** {Application.unityVersion}");
        logBuffer.AppendLine($"**Platform:** {Application.platform}");
        logBuffer.AppendLine($"**Quality Level:** {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
        logBuffer.AppendLine($"**Screen Resolution:** {Screen.width}x{Screen.height}");
        logBuffer.AppendLine($"**Target Frame Rate:** {Application.targetFrameRate}");
        logBuffer.AppendLine();
        logBuffer.AppendLine("---");
        logBuffer.AppendLine();
        logBuffer.AppendLine("## Performance Timeline");
        logBuffer.AppendLine();
        logBuffer.AppendLine("| Time | FPS | Frame (ms) | Memory (MB) | Object | Materials | Notes |");
        logBuffer.AppendLine("|------|-----|------------|-------------|--------|-----------|-------|");

        headerWritten = true;
        FlushLogBuffer();
    }

    private void UpdatePerformanceMetrics()
    {
        float currentFrameTime = Time.unscaledDeltaTime * 1000f;
        float currentFPS = 1f / Time.unscaledDeltaTime;

        smoothedFrameTime += (currentFrameTime - smoothedFrameTime) * 0.1f;
        smoothedFPS += (currentFPS - smoothedFPS) * 0.1f;

        // Track frame history
        FrameData frame = new FrameData
        {
            time = Time.realtimeSinceStartup - sessionStartTime,
            frameTimeMs = currentFrameTime,
            fps = currentFPS,
            lookedAtObject = lastLookedAtObject != null ? lastLookedAtObject.name : "None",
            materials = lastLookedAtMaterials,
            memoryBytes = Profiler.GetTotalAllocatedMemoryLong()
        };

        frameHistory.Add(frame);
        if (frameHistory.Count > FRAME_HISTORY_SIZE)
        {
            frameHistory.RemoveAt(0);
        }

        // Track performance correlation with materials
        if (!string.IsNullOrEmpty(lastLookedAtMaterials))
        {
            string[] mats = lastLookedAtMaterials.Split(',');
            foreach (string mat in mats)
            {
                string matName = mat.Trim();
                if (string.IsNullOrEmpty(matName)) continue;

                if (!materialPerformanceMap.ContainsKey(matName))
                {
                    materialPerformanceMap[matName] = new MaterialPerformanceData { materialName = matName };
                }

                var data = materialPerformanceMap[matName];
                data.totalFramesVisible++;
                data.avgFrameTimeWhileVisible += (currentFrameTime - data.avgFrameTimeWhileVisible) / data.totalFramesVisible;
                data.maxFrameTimeWhileVisible = Mathf.Max(data.maxFrameTimeWhileVisible, currentFrameTime);

                if (currentFrameTime > frameSpikeThresholdMs)
                {
                    data.frameSpikesWhileVisible++;
                }
            }
        }
    }

    private void UpdateLookedAtObject()
    {
        if (!logLookedAtObject || mainCamera == null)
        {
            mainCamera = Camera.main;
            return;
        }

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, raycastLayers))
        {
            lastLookedAtObject = hit.collider.gameObject;

            // Get materials from the object
            Renderer renderer = hit.collider.GetComponent<Renderer>();
            if (renderer == null)
            {
                renderer = hit.collider.GetComponentInChildren<Renderer>();
            }

            if (renderer != null && renderer.sharedMaterials != null)
            {
                var matNames = new List<string>();
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat != null)
                    {
                        string matInfo = mat.name;
                        if (mat.shader != null)
                        {
                            matInfo += $" ({mat.shader.name})";
                        }
                        matNames.Add(matInfo);
                    }
                }
                lastLookedAtMaterials = string.Join(", ", matNames);
            }
            else
            {
                lastLookedAtMaterials = "-";
            }
        }
        else
        {
            lastLookedAtObject = null;
            lastLookedAtMaterials = "";
        }
    }

    private void ScanForHeavyObjects()
    {
        if (!logHeaviestObjects) return;

        heavyObjects.Clear();

        Renderer[] allRenderers = FindObjectsOfType<Renderer>();

        foreach (var renderer in allRenderers)
        {
            if (renderer == null || !renderer.gameObject.activeInHierarchy) continue;

            // Get material and shader info
            List<string> matNames = new List<string>();
            List<string> shaderNames = new List<string>();

            if (renderer.sharedMaterials != null)
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat != null)
                    {
                        matNames.Add(mat.name);
                        if (mat.shader != null)
                        {
                            shaderNames.Add(mat.shader.name);
                        }
                    }
                }
            }

            HeavyObjectData data = new HeavyObjectData
            {
                gameObject = renderer.gameObject,
                name = GetFullPath(renderer.gameObject),
                materialNames = matNames.ToArray(),
                shaderNames = string.Join(", ", shaderNames.Distinct()),
                materialCount = renderer.sharedMaterials != null ? renderer.sharedMaterials.Length : 0,
                memoryEstimate = 0,
                vertexCount = 0,
                triangleCount = 0
            };

            // Get mesh data
            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            SkinnedMeshRenderer skinnedMesh = renderer as SkinnedMeshRenderer;

            Mesh mesh = null;
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                mesh = meshFilter.sharedMesh;
            }
            else if (skinnedMesh != null && skinnedMesh.sharedMesh != null)
            {
                mesh = skinnedMesh.sharedMesh;
            }

            if (mesh != null)
            {
                try
                {
                    data.vertexCount = mesh.vertexCount;
                    data.triangleCount = mesh.subMeshCount > 0 ? (int)(mesh.GetIndexCount(0) / 3) : 0;
                    data.memoryEstimate = Profiler.GetRuntimeMemorySizeLong(mesh);
                }
                catch
                {
                    data.vertexCount = mesh.vertexCount;
                    data.triangleCount = mesh.vertexCount / 3;
                }
            }

            // Calculate render weight - heavier weight for complex shaders
            float shaderComplexity = 1f;
            if (data.shaderNames.Contains("Lit")) shaderComplexity = 1.5f;
            if (data.shaderNames.Contains("PBR")) shaderComplexity = 2f;
            if (data.shaderNames.Contains("Particle")) shaderComplexity = 1.8f;
            if (data.shaderNames.Contains("Transparent")) shaderComplexity = 2f;
            if (data.shaderNames.Contains("PostProcess") || data.shaderNames.Contains("FullScreen")) shaderComplexity = 3f;

            data.renderWeight = (data.vertexCount * 0.001f +
                               data.triangleCount * 0.002f +
                               data.materialCount * 10f +
                               renderer.bounds.size.magnitude * 5f) * shaderComplexity;

            heavyObjects.Add(data);
        }

        heavyObjects = heavyObjects.OrderByDescending(o => o.renderWeight).Take(topHeavyObjectCount * 2).ToList();
    }

    private void LogPerformanceEntry()
    {
        if (entryCount >= maxLogEntries) return;

        float sessionTime = Time.realtimeSinceStartup - sessionStartTime;
        string timeStr = TimeSpan.FromSeconds(sessionTime).ToString(@"mm\:ss");
        float memoryMB = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);

        string lookedAt = lastLookedAtObject != null ? lastLookedAtObject.name : "-";
        if (lookedAt.Length > 20) lookedAt = lookedAt.Substring(0, 17) + "...";

        string materials = lastLookedAtMaterials;
        if (materials.Length > 30) materials = materials.Substring(0, 27) + "...";
        if (string.IsNullOrEmpty(materials)) materials = "-";

        // Determine notes based on performance
        string notes = "";
        if (smoothedFrameTime > frameSpikeThresholdMs)
        {
            notes = "⚠️ SLOW";
        }
        else if (smoothedFPS >= 58)
        {
            notes = "✓";
        }

        string entry = $"| {timeStr} | {smoothedFPS:F1} | {smoothedFrameTime:F2} | {memoryMB:F1} | {lookedAt} | {materials} | {notes} |";
        logBuffer.AppendLine(entry);
        entryCount++;

        if (entryCount % 10 == 0)
        {
            FlushLogBuffer();
        }
    }

    private void WriteSummaryAndClose()
    {
        if (!headerWritten) return;

        logBuffer.AppendLine();
        logBuffer.AppendLine("---");
        logBuffer.AppendLine();
        logBuffer.AppendLine("## Session Summary");
        logBuffer.AppendLine();

        float sessionDuration = Time.realtimeSinceStartup - sessionStartTime;
        logBuffer.AppendLine($"**Total Duration:** {TimeSpan.FromSeconds(sessionDuration):hh\\:mm\\:ss}");
        logBuffer.AppendLine($"**Log Entries:** {entryCount}");
        logBuffer.AppendLine();

        // Frame rate statistics
        if (frameHistory.Count > 0)
        {
            float avgFPS = frameHistory.Average(f => f.fps);
            float avgFrameTime = frameHistory.Average(f => f.frameTimeMs);
            float minFPS = frameHistory.Min(f => f.fps);
            float maxFPS = frameHistory.Max(f => f.fps);
            int spikeCount = frameHistory.Count(f => f.frameTimeMs > frameSpikeThresholdMs);

            logBuffer.AppendLine("### Frame Rate Statistics");
            logBuffer.AppendLine();
            logBuffer.AppendLine($"- **Average FPS:** {avgFPS:F1}");
            logBuffer.AppendLine($"- **Min FPS:** {minFPS:F1}");
            logBuffer.AppendLine($"- **Max FPS:** {maxFPS:F1}");
            logBuffer.AppendLine($"- **Average Frame Time:** {avgFrameTime:F2}ms");
            logBuffer.AppendLine($"- **Frame Spikes (>{frameSpikeThresholdMs:F1}ms):** {spikeCount}");
            logBuffer.AppendLine();
        }

        // Heavy objects with materials
        if (heavyObjects.Count > 0)
        {
            logBuffer.AppendLine("### Heaviest Objects (by render weight)");
            logBuffer.AppendLine();
            logBuffer.AppendLine("| Object | Verts | Tris | Materials | Shaders | Weight |");
            logBuffer.AppendLine("|--------|-------|------|-----------|---------|--------|");

            foreach (var obj in heavyObjects.Take(topHeavyObjectCount))
            {
                string name = obj.name.Length > 30 ? obj.name.Substring(0, 27) + "..." : obj.name;
                string shaders = obj.shaderNames.Length > 25 ? obj.shaderNames.Substring(0, 22) + "..." : obj.shaderNames;
                logBuffer.AppendLine($"| {name} | {obj.vertexCount:N0} | {obj.triangleCount:N0} | {obj.materialCount} | {shaders} | {obj.renderWeight:F1} |");
            }
            logBuffer.AppendLine();
        }

        // Materials correlated with performance issues
        if (materialPerformanceMap.Count > 0)
        {
            var problematicMaterials = materialPerformanceMap.Values
                .Where(m => m.frameSpikesWhileVisible > 0)
                .OrderByDescending(m => m.frameSpikesWhileVisible)
                .Take(15)
                .ToList();

            if (problematicMaterials.Count > 0)
            {
                logBuffer.AppendLine("### Materials Correlated with Frame Spikes");
                logBuffer.AppendLine();
                logBuffer.AppendLine("| Material (Shader) | Spikes | Frames Visible | Avg Frame Time | Max Frame Time |");
                logBuffer.AppendLine("|-------------------|--------|----------------|----------------|----------------|");

                foreach (var mat in problematicMaterials)
                {
                    string name = mat.materialName;
                    if (name.Length > 40) name = name.Substring(0, 37) + "...";
                    logBuffer.AppendLine($"| {name} | {mat.frameSpikesWhileVisible} | {mat.totalFramesVisible} | {mat.avgFrameTimeWhileVisible:F2}ms | {mat.maxFrameTimeWhileVisible:F2}ms |");
                }
                logBuffer.AppendLine();
            }

            // Also show most frequently seen materials
            var frequentMaterials = materialPerformanceMap.Values
                .OrderByDescending(m => m.totalFramesVisible)
                .Take(10)
                .ToList();

            logBuffer.AppendLine("### Most Viewed Materials");
            logBuffer.AppendLine();
            logBuffer.AppendLine("| Material (Shader) | Frames Visible | Avg Frame Time |");
            logBuffer.AppendLine("|-------------------|----------------|----------------|");

            foreach (var mat in frequentMaterials)
            {
                string name = mat.materialName;
                if (name.Length > 45) name = name.Substring(0, 42) + "...";
                logBuffer.AppendLine($"| {name} | {mat.totalFramesVisible} | {mat.avgFrameTimeWhileVisible:F2}ms |");
            }
            logBuffer.AppendLine();
        }

        logBuffer.AppendLine("---");
        logBuffer.AppendLine();
        logBuffer.AppendLine($"*Log generated at {DateTime.Now:yyyy-MM-dd HH:mm:ss}*");

        FlushLogBuffer();
        Debug.Log($"[PerformanceLogger] Log saved to: {logFilePath}");
    }

    private void FlushLogBuffer()
    {
        if (string.IsNullOrEmpty(logFilePath))
        {
            Debug.LogError("[PerformanceLogger] Log path not initialized!");
            return;
        }

        string content = logBuffer.ToString();
        if (string.IsNullOrEmpty(content)) return;

        try
        {
            File.AppendAllText(logFilePath, content);
            logBuffer.Clear();
        }
        catch (Exception e)
        {
            Debug.LogError($"[PerformanceLogger] Failed to write log: {e.Message}\nPath: {logFilePath}");
        }
    }

    private string GetFullPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        int depth = 0;
        while (parent != null && depth < 3)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
            depth++;
        }
        return path;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        if (FindObjectOfType<PerformanceLogger>() == null)
        {
            GameObject go = new GameObject("[Performance Logger]");
            go.AddComponent<PerformanceLogger>();
        }
#endif
    }
}
