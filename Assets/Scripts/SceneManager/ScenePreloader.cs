using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SceneManagement
{
    /// <summary>
    /// Preloads scenes in the background without blocking gameplay
    /// This eliminates loading pauses and creates seamless transitions
    /// </summary>
    public class ScenePreloader : MonoBehaviour
    {
        [Header("Preload Queue")]
        [Tooltip("Scenes will be preloaded in this order")]
        [SerializeField] private List<ScenePreloadData> scenesToPreload = new List<ScenePreloadData>();

        [Header("Settings")]
        [Tooltip("Start preloading automatically on Start()")]
        [SerializeField] private bool autoPreload = true;

        [Tooltip("Maximum number of scenes to keep preloaded at once")]
        [SerializeField] private int maxPreloadedScenes = 2;

        [Tooltip("Delay before starting preload (gives current scene time to settle)")]
        [SerializeField] private float preloadDelay = 1f;

        [Header("Activation Performance")]
        [Tooltip("Number of GameObjects to activate per frame (lower = smoother, higher = faster)")]
        [SerializeField] private int objectsPerFrame = 5;

        [Tooltip("Wait extra frames between batches to reduce hitching")]
        [SerializeField] private int extraFramesBetweenBatches = 1;

        [Tooltip("Analyze scene hierarchy after preloading to identify bottlenecks")]
        [SerializeField] private bool analyzeHierarchy = true;

        [Header("Heavy Scene Optimization")]
        [Tooltip("Use time-sliced operations for scenes with more GameObjects than this threshold")]
        [SerializeField] private int heavySceneThreshold = 5000;

        [Tooltip("Maximum milliseconds per frame for heavy scene operations (8ms recommended for 60 FPS)")]
        [SerializeField] private float maxMillisecondsPerFrame = 8f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // Singleton instance
        private static ScenePreloader instance;
        public static ScenePreloader Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<ScenePreloader>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("ScenePreloader");
                        instance = go.AddComponent<ScenePreloader>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        // Track preloaded scenes
        private Dictionary<string, AsyncOperation> preloadedScenes = new Dictionary<string, AsyncOperation>();
        private Queue<string> preloadQueue = new Queue<string>();
        private bool isPreloading = false;

        private void Awake()
        {
            // Singleton pattern
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (autoPreload && scenesToPreload.Count > 0)
            {
                StartCoroutine(AutoPreloadSequence());
            }
        }

        /// <summary>
        /// Preload a scene in the background without activating it
        /// </summary>
        public void PreloadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("Scene name is empty!");
                return;
            }

            if (preloadedScenes.ContainsKey(sceneName))
            {
                if (showDebugLogs)
                    Debug.Log($"Scene '{sceneName}' is already preloaded or preloading.");
                return;
            }

            preloadQueue.Enqueue(sceneName);

            if (!isPreloading)
            {
                StartCoroutine(PreloadNextInQueue());
            }
        }

        /// <summary>
        /// Preload multiple scenes
        /// </summary>
        public void PreloadScenes(params string[] sceneNames)
        {
            foreach (string sceneName in sceneNames)
            {
                PreloadScene(sceneName);
            }
        }

        /// <summary>
        /// Activate a preloaded scene instantly (seamless transition)
        /// </summary>
        public void ActivatePreloadedScene(string sceneName)
        {
            if (!preloadedScenes.ContainsKey(sceneName))
            {
                Debug.LogError($"Scene '{sceneName}' is not preloaded! Call PreloadScene() first.");
                return;
            }

            AsyncOperation asyncOp = preloadedScenes[sceneName];

            if (asyncOp.progress >= 0.9f)
            {
                // Get the current active scene before switching
                Scene currentScene = SceneManager.GetActiveScene();

                // Scene is ready - activate it instantly!
                asyncOp.allowSceneActivation = true;
                preloadedScenes.Remove(sceneName);

                // Start coroutine to set as active scene and unload old scene after activation
                StartCoroutine(SwitchToNewScene(sceneName, currentScene.name));

                if (showDebugLogs)
                    Debug.Log($"Activated preloaded scene: {sceneName}");
            }
            else
            {
                Debug.LogWarning($"Scene '{sceneName}' is still loading. Activating anyway...");
                StartCoroutine(WaitAndActivate(sceneName, asyncOp));
            }
        }

        /// <summary>
        /// Check if a scene is preloaded and ready
        /// </summary>
        public bool IsSceneReady(string sceneName)
        {
            if (preloadedScenes.TryGetValue(sceneName, out AsyncOperation asyncOp))
            {
                return asyncOp.progress >= 0.9f;
            }
            return false;
        }

        /// <summary>
        /// Get loading progress for a scene (0-1)
        /// </summary>
        public float GetSceneLoadProgress(string sceneName)
        {
            if (preloadedScenes.TryGetValue(sceneName, out AsyncOperation asyncOp))
            {
                return Mathf.Clamp01(asyncOp.progress / 0.9f);
            }
            return 0f;
        }

        /// <summary>
        /// Unload a preloaded scene to free memory
        /// </summary>
        public void UnloadPreloadedScene(string sceneName)
        {
            if (preloadedScenes.ContainsKey(sceneName))
            {
                preloadedScenes.Remove(sceneName);

                if (showDebugLogs)
                    Debug.Log($"Unloaded preloaded scene: {sceneName}");
            }
        }

        /// <summary>
        /// Clear all preloaded scenes
        /// </summary>
        public void ClearAllPreloaded()
        {
            preloadedScenes.Clear();
            preloadQueue.Clear();

            if (showDebugLogs)
                Debug.Log("Cleared all preloaded scenes");
        }

        private IEnumerator AutoPreloadSequence()
        {
            yield return new WaitForSeconds(preloadDelay);

            foreach (var sceneData in scenesToPreload)
            {
#if UNITY_EDITOR
                string sceneName = sceneData.sceneAsset != null
                    ? System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(sceneData.sceneAsset))
                    : sceneData.sceneName;
#else
                string sceneName = sceneData.sceneName;
#endif

                if (!string.IsNullOrEmpty(sceneName))
                {
                    PreloadScene(sceneName);

                    if (sceneData.delayBetweenPreloads > 0)
                    {
                        yield return new WaitForSeconds(sceneData.delayBetweenPreloads);
                    }
                }
            }
        }

        private IEnumerator PreloadNextInQueue()
        {
            isPreloading = true;

            while (preloadQueue.Count > 0)
            {
                // Check if we've hit the max preloaded scenes limit
                if (preloadedScenes.Count >= maxPreloadedScenes)
                {
                    if (showDebugLogs)
                        Debug.Log($"Max preloaded scenes ({maxPreloadedScenes}) reached. Waiting...");

                    yield return new WaitForSeconds(1f);
                    continue;
                }

                string sceneName = preloadQueue.Dequeue();

                if (showDebugLogs)
                    Debug.Log($"Starting background preload: {sceneName}");

                // Wait multiple frames to ensure we don't block the current frame
                yield return null;
                yield return null;

                // Lower loading priority to minimize frame hitches
                Application.backgroundLoadingPriority = ThreadPriority.Low;

                // Start loading the scene asynchronously (Additive = no freeze!)
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                asyncLoad.allowSceneActivation = false; // Don't activate yet!
                asyncLoad.priority = 0; // Lowest priority - load slowly over many frames

                // Store the async operation
                preloadedScenes[sceneName] = asyncLoad;

                if (showDebugLogs)
                    Debug.Log($"Loading scene '{sceneName}' with lowest priority over multiple frames...");

                // Wait until scene is fully loaded in background with progress monitoring
                float lastProgress = 0f;
                while (asyncLoad.progress < 0.9f)
                {
                    // Log progress for heavy scenes
                    if (showDebugLogs && asyncLoad.progress - lastProgress > 0.1f)
                    {
                        Debug.Log($"Scene '{sceneName}' loading progress: {asyncLoad.progress:P0}");
                        lastProgress = asyncLoad.progress;
                    }

                    // Yield every frame to keep game responsive
                    yield return null;
                }

                // CRITICAL: Wait several frames for scene to fully instantiate before disabling
                // This gives Unity time to complete background operations
                yield return null;
                yield return null;
                yield return null;

                Scene loadedScene = SceneManager.GetSceneByName(sceneName);
                if (loadedScene.isLoaded)
                {
                    GameObject[] rootObjects = loadedScene.GetRootGameObjects();

                    // Count total objects in scene to determine if it's heavy
                    int totalObjects = CountSceneObjects(loadedScene);
                    bool isHeavyScene = totalObjects >= heavySceneThreshold;

                    if (showDebugLogs)
                    {
                        Debug.Log($"Scene '{sceneName}' has {totalObjects} total GameObjects");
                        Debug.Log($"Disabling {rootObjects.Length} root objects (Heavy scene optimization: {isHeavyScene})");
                    }

                    // Use time-sliced operations for heavy scenes
                    if (isHeavyScene)
                    {
                        HeavySceneOptimizer optimizer = GetOrCreateHeavyOptimizer();
                        yield return StartCoroutine(optimizer.DisableSceneObjectsGradually(loadedScene));
                    }
                    else
                    {
                        // Standard disabling for lighter scenes
                        int objectsDisabledPerFrame = Mathf.Max(1, rootObjects.Length / 10);
                        for (int i = 0; i < rootObjects.Length; i++)
                        {
                            rootObjects[i].SetActive(false);

                            if ((i + 1) % objectsDisabledPerFrame == 0)
                            {
                                yield return null;
                            }
                        }

                        if (showDebugLogs)
                            Debug.Log($"All {rootObjects.Length} root objects disabled in scene: {sceneName}");
                    }

                    // Analyze scene hierarchy to identify performance bottlenecks
                    if (analyzeHierarchy)
                    {
                        yield return null; // Wait a frame before analysis

                        SceneHierarchyAnalyzer analyzer = FindObjectOfType<SceneHierarchyAnalyzer>();
                        if (analyzer == null)
                        {
                            GameObject go = new GameObject("SceneHierarchyAnalyzer_Temp");
                            analyzer = go.AddComponent<SceneHierarchyAnalyzer>();
                            analyzer.AnalyzeScene(loadedScene);
                            Destroy(go);
                        }
                        else
                        {
                            analyzer.AnalyzeScene(loadedScene);
                        }
                    }
                }

                // Restore normal loading priority
                Application.backgroundLoadingPriority = ThreadPriority.Normal;

                if (showDebugLogs)
                    Debug.Log($"Preloaded and ready (objects disabled): {sceneName} (Progress: {asyncLoad.progress:P0})");
            }

            isPreloading = false;
        }

        private IEnumerator WaitAndActivate(string sceneName, AsyncOperation asyncOp)
        {
            // Wait until scene is ready
            while (asyncOp.progress < 0.9f)
            {
                yield return null;
            }

            asyncOp.allowSceneActivation = true;
            preloadedScenes.Remove(sceneName);

            if (showDebugLogs)
                Debug.Log($"Activated scene after waiting: {sceneName}");
        }

        private IEnumerator SwitchToNewScene(string newSceneName, string oldSceneName)
        {
            // Wait one frame for the new scene to fully activate
            yield return null;

            // Get the newly loaded scene
            Scene newScene = SceneManager.GetSceneByName(newSceneName);

            if (newScene.isLoaded)
            {
                // Check if this is a heavy scene
                int totalObjects = CountSceneObjects(newScene);
                bool isHeavyScene = totalObjects >= heavySceneThreshold;

                if (showDebugLogs)
                    Debug.Log($"Re-enabling objects in scene: {newSceneName} (Total objects: {totalObjects}, Heavy: {isHeavyScene})");

                // Use time-sliced operations for heavy scenes
                if (isHeavyScene)
                {
                    HeavySceneOptimizer optimizer = GetOrCreateHeavyOptimizer();
                    yield return StartCoroutine(optimizer.EnableSceneObjectsGradually(newScene, objectsPerFrame));
                }
                else
                {
                    // Standard enabling for lighter scenes
                    GameObject[] rootObjects = newScene.GetRootGameObjects();
                    for (int i = 0; i < rootObjects.Length; i++)
                    {
                        rootObjects[i].SetActive(true);

                        if ((i + 1) % objectsPerFrame == 0)
                        {
                            for (int f = 0; f < extraFramesBetweenBatches; f++)
                            {
                                yield return null;
                            }
                        }
                    }
                }

                // Wait one final frame before setting as active
                yield return null;

                // Set the new scene as the active scene
                SceneManager.SetActiveScene(newScene);

                if (showDebugLogs)
                    Debug.Log($"Set active scene to: {newSceneName}");

                // Unload the old scene asynchronously
                if (!string.IsNullOrEmpty(oldSceneName) && oldSceneName != newSceneName)
                {
                    AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(oldSceneName);

                    if (showDebugLogs)
                        Debug.Log($"Unloading old scene: {oldSceneName}");

                    // Optionally wait for unload to complete
                    while (unloadOp != null && !unloadOp.isDone)
                    {
                        yield return null;
                    }

                    if (showDebugLogs)
                        Debug.Log($"Old scene unloaded: {oldSceneName}");
                }
            }
            else
            {
                Debug.LogError($"Failed to load new scene: {newSceneName}");
            }
        }

        // Helper methods
        private int CountSceneObjects(Scene scene)
        {
            if (!scene.isLoaded) return 0;

            int count = 0;
            GameObject[] rootObjects = scene.GetRootGameObjects();

            foreach (GameObject root in rootObjects)
            {
                count += CountHierarchy(root.transform);
            }

            return count;
        }

        private int CountHierarchy(Transform transform)
        {
            int count = 1;
            for (int i = 0; i < transform.childCount; i++)
            {
                count += CountHierarchy(transform.GetChild(i));
            }
            return count;
        }

        private HeavySceneOptimizer heavyOptimizer;
        private HeavySceneOptimizer GetOrCreateHeavyOptimizer()
        {
            if (heavyOptimizer == null)
            {
                heavyOptimizer = gameObject.AddComponent<HeavySceneOptimizer>();
            }
            return heavyOptimizer;
        }

        // Static helper methods
        public static void PreloadSceneStatic(string sceneName)
        {
            Instance.PreloadScene(sceneName);
        }

        public static void ActivateSceneStatic(string sceneName)
        {
            Instance.ActivatePreloadedScene(sceneName);
        }

        public static bool IsSceneReadyStatic(string sceneName)
        {
            return Instance.IsSceneReady(sceneName);
        }
    }

    /// <summary>
    /// Data class for scene preload configuration
    /// </summary>
    [System.Serializable]
    public class ScenePreloadData
    {
#if UNITY_EDITOR
        [Tooltip("Drag scene asset here")]
        public SceneAsset sceneAsset;
#endif

        [Tooltip("Or manually enter scene name")]
        public string sceneName;

        [Tooltip("Delay before preloading next scene (0 = immediate)")]
        public float delayBetweenPreloads = 0.5f;
    }
}
