using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SceneManagement
{
    /// <summary>
    /// Advanced optimizer for very heavy scenes (10K+ GameObjects)
    /// Implements time-sliced loading with frame budget monitoring
    /// </summary>
    public class HeavySceneOptimizer : MonoBehaviour
    {
        [Header("Performance Budget")]
        [Tooltip("Maximum milliseconds per frame during scene operations (16.67ms = 60 FPS)")]
        [SerializeField] private float maxMillisecondsPerFrame = 8f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        private System.Diagnostics.Stopwatch frameTimer = new System.Diagnostics.Stopwatch();

        /// <summary>
        /// Disables all GameObjects in a scene with time-slicing to prevent frame hitches
        /// </summary>
        public IEnumerator DisableSceneObjectsGradually(Scene scene)
        {
            if (!scene.isLoaded)
            {
                Debug.LogError($"Scene {scene.name} is not loaded!");
                yield break;
            }

            GameObject[] rootObjects = scene.GetRootGameObjects();

            if (showDebugLogs)
                Debug.Log($"[HeavySceneOptimizer] Disabling {rootObjects.Length} root objects in scene: {scene.name}");

            frameTimer.Restart();
            int disabledCount = 0;

            foreach (GameObject rootObj in rootObjects)
            {
                // Disable the object
                rootObj.SetActive(false);
                disabledCount++;

                // Check if we've exceeded our frame budget
                if (frameTimer.Elapsed.TotalMilliseconds > maxMillisecondsPerFrame)
                {
                    if (showDebugLogs)
                        Debug.Log($"[HeavySceneOptimizer] Frame budget exceeded ({frameTimer.Elapsed.TotalMilliseconds:F2}ms), yielding... ({disabledCount}/{rootObjects.Length} disabled)");

                    yield return null;
                    frameTimer.Restart();
                }
            }

            if (showDebugLogs)
                Debug.Log($"[HeavySceneOptimizer] Completed disabling {disabledCount} objects in scene: {scene.name}");
        }

        /// <summary>
        /// Enables scene objects gradually with time-slicing
        /// </summary>
        public IEnumerator EnableSceneObjectsGradually(Scene scene, int objectsPerFrame = 5)
        {
            if (!scene.isLoaded)
            {
                Debug.LogError($"Scene {scene.name} is not loaded!");
                yield break;
            }

            GameObject[] rootObjects = scene.GetRootGameObjects();

            if (showDebugLogs)
                Debug.Log($"[HeavySceneOptimizer] Enabling {rootObjects.Length} root objects ({objectsPerFrame} per frame)");

            frameTimer.Restart();
            int enabledCount = 0;

            foreach (GameObject rootObj in rootObjects)
            {
                rootObj.SetActive(true);
                enabledCount++;

                // Enable in batches
                if (enabledCount % objectsPerFrame == 0)
                {
                    if (showDebugLogs)
                        Debug.Log($"[HeavySceneOptimizer] Enabled batch ({enabledCount}/{rootObjects.Length}), frame time: {frameTimer.Elapsed.TotalMilliseconds:F2}ms");

                    yield return null;
                    frameTimer.Restart();
                }
            }

            if (showDebugLogs)
                Debug.Log($"[HeavySceneOptimizer] Completed enabling {enabledCount} objects");
        }

        /// <summary>
        /// Analyzes scene to identify what's making it heavy
        /// </summary>
        public SceneHeavyAnalysis AnalyzeHeavyScene(Scene scene)
        {
            if (!scene.isLoaded)
            {
                Debug.LogWarning($"Scene {scene.name} is not loaded");
                return null;
            }

            SceneHeavyAnalysis analysis = new SceneHeavyAnalysis { sceneName = scene.name };

            GameObject[] rootObjects = scene.GetRootGameObjects();
            analysis.totalRootObjects = rootObjects.Length;

            // Analyze each root hierarchy
            foreach (GameObject root in rootObjects)
            {
                AnalyzeHierarchyRecursive(root.transform, analysis);
            }

            // Sort to find heaviest objects
            analysis.heaviestHierarchies = analysis.hierarchyWeights
                .OrderByDescending(kvp => kvp.Value)
                .Take(10)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            PrintHeavyAnalysis(analysis);
            return analysis;
        }

        private void AnalyzeHierarchyRecursive(Transform transform, SceneHeavyAnalysis analysis)
        {
            analysis.totalGameObjects++;

            // Count components
            Component[] components = transform.GetComponents<Component>();
            analysis.totalComponents += components.Length;

            // Track heaviest component types
            foreach (var component in components)
            {
                if (component == null) continue;

                string typeName = component.GetType().Name;
                if (!analysis.componentCounts.ContainsKey(typeName))
                    analysis.componentCounts[typeName] = 0;
                analysis.componentCounts[typeName]++;
            }

            // Recursively analyze children
            int childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                AnalyzeHierarchyRecursive(transform.GetChild(i), analysis);
            }

            // Track weight of each root hierarchy
            if (transform.parent == null)
            {
                int hierarchySize = CountHierarchySize(transform);
                analysis.hierarchyWeights[transform.name] = hierarchySize;
            }
        }

        private int CountHierarchySize(Transform transform)
        {
            int count = 1;
            for (int i = 0; i < transform.childCount; i++)
            {
                count += CountHierarchySize(transform.GetChild(i));
            }
            return count;
        }

        private void PrintHeavyAnalysis(SceneHeavyAnalysis analysis)
        {
            Debug.Log($"=== HEAVY SCENE ANALYSIS: {analysis.sceneName} ===");
            Debug.Log($"Total GameObjects: {analysis.totalGameObjects}");
            Debug.Log($"Total Components: {analysis.totalComponents}");
            Debug.Log($"Root Objects: {analysis.totalRootObjects}");

            Debug.Log("\n--- TOP 10 HEAVIEST HIERARCHIES ---");
            foreach (var kvp in analysis.heaviestHierarchies)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value} GameObjects");
            }

            Debug.Log("\n--- TOP 10 MOST COMMON COMPONENTS ---");
            var topComponents = analysis.componentCounts
                .OrderByDescending(kvp => kvp.Value)
                .Take(10);

            foreach (var kvp in topComponents)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value} instances");
            }

            // Suggestions
            Debug.Log("\n--- OPTIMIZATION SUGGESTIONS ---");

            if (analysis.totalGameObjects > 10000)
            {
                Debug.LogWarning("⚠ Scene has 10K+ objects - consider splitting into subscenes");
            }

            if (analysis.hierarchyWeights.Values.Any(v => v > 5000))
            {
                Debug.LogWarning("⚠ Some hierarchies have 5K+ objects - consider breaking them up");
                Debug.LogWarning("  Heaviest hierarchies:");
                foreach (var kvp in analysis.heaviestHierarchies.Take(3))
                {
                    Debug.LogWarning($"    - {kvp.Key}: {kvp.Value} objects");
                }
            }

            if (analysis.componentCounts.ContainsKey("MeshRenderer") &&
                analysis.componentCounts["MeshRenderer"] > 1000)
            {
                Debug.LogWarning($"⚠ {analysis.componentCounts["MeshRenderer"]} MeshRenderers - consider static batching");
            }

            Debug.Log("=== END HEAVY ANALYSIS ===\n");
        }
    }

    [System.Serializable]
    public class SceneHeavyAnalysis
    {
        public string sceneName;
        public int totalGameObjects;
        public int totalComponents;
        public int totalRootObjects;
        public Dictionary<string, int> hierarchyWeights = new Dictionary<string, int>();
        public Dictionary<string, int> componentCounts = new Dictionary<string, int>();
        public Dictionary<string, int> heaviestHierarchies = new Dictionary<string, int>();
    }
}
