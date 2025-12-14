using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

namespace SceneManagement
{
    /// <summary>
    /// Analyzes scene hierarchy to identify performance bottlenecks
    /// Based on Unity best practices: max 50 children per object, max 4 levels deep
    /// </summary>
    public class SceneHierarchyAnalyzer : MonoBehaviour
    {
        [Header("Analysis Settings")]
        [SerializeField] private bool analyzeOnStart = false;
        [SerializeField] private bool showDetailedReport = true;

        [Header("Thresholds (Unity Best Practices)")]
        [SerializeField] private int maxChildrenPerObject = 50;
        [SerializeField] private int maxHierarchyDepth = 4;

        private void Start()
        {
            if (analyzeOnStart)
            {
                AnalyzeCurrentScene();
            }
        }

        [ContextMenu("Analyze Current Scene")]
        public void AnalyzeCurrentScene()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            AnalyzeScene(currentScene);
        }

        [ContextMenu("Analyze All Loaded Scenes")]
        public void AnalyzeAllScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    AnalyzeScene(scene);
                }
            }
        }

        public SceneAnalysisReport AnalyzeScene(Scene scene)
        {
            if (!scene.isLoaded)
            {
                Debug.LogWarning($"Scene '{scene.name}' is not loaded. Skipping analysis.");
                return null;
            }

            SceneAnalysisReport report = new SceneAnalysisReport
            {
                sceneName = scene.name
            };

            GameObject[] rootObjects = scene.GetRootGameObjects();
            report.totalRootObjects = rootObjects.Length;
            report.totalGameObjects = 0;

            foreach (GameObject root in rootObjects)
            {
                AnalyzeHierarchy(root.transform, 0, report);
            }

            PrintReport(report);
            return report;
        }

        private void AnalyzeHierarchy(Transform transform, int currentDepth, SceneAnalysisReport report)
        {
            report.totalGameObjects++;

            // Track depth
            if (currentDepth > report.maxDepthFound)
            {
                report.maxDepthFound = currentDepth;
                report.deepestObjectPath = GetTransformPath(transform);
            }

            // Check for depth violations
            if (currentDepth > maxHierarchyDepth)
            {
                report.depthViolations++;
                if (report.depthViolationExamples.Count < 5)
                {
                    report.depthViolationExamples.Add($"{GetTransformPath(transform)} (depth: {currentDepth})");
                }
            }

            // Check for child count violations
            int childCount = transform.childCount;
            if (childCount > report.maxChildrenFound)
            {
                report.maxChildrenFound = childCount;
                report.objectWithMostChildren = GetTransformPath(transform);
            }

            if (childCount > maxChildrenPerObject)
            {
                report.childCountViolations++;
                if (report.childCountViolationExamples.Count < 5)
                {
                    report.childCountViolationExamples.Add($"{GetTransformPath(transform)} ({childCount} children)");
                }
            }

            // Analyze components
            Component[] components = transform.GetComponents<Component>();
            report.totalComponents += components.Length;

            // Recursively analyze children
            for (int i = 0; i < childCount; i++)
            {
                AnalyzeHierarchy(transform.GetChild(i), currentDepth + 1, report);
            }
        }

        private string GetTransformPath(Transform transform)
        {
            string path = transform.name;
            Transform parent = transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        private void PrintReport(SceneAnalysisReport report)
        {
            Debug.Log($"=== SCENE HIERARCHY ANALYSIS: {report.sceneName} ===");
            Debug.Log($"Total Root GameObjects: {report.totalRootObjects}");
            Debug.Log($"Total GameObjects: {report.totalGameObjects}");
            Debug.Log($"Total Components: {report.totalComponents}");
            Debug.Log($"Max Hierarchy Depth: {report.maxDepthFound} (recommended: ≤{maxHierarchyDepth})");
            Debug.Log($"Max Children Per Object: {report.maxChildrenFound} (recommended: ≤{maxChildrenPerObject})");

            if (report.depthViolations > 0)
            {
                Debug.LogWarning($"⚠ DEPTH VIOLATIONS: {report.depthViolations} objects exceed depth limit of {maxHierarchyDepth}");
                if (showDetailedReport && report.depthViolationExamples.Count > 0)
                {
                    Debug.LogWarning("Examples of deep hierarchies:");
                    foreach (string example in report.depthViolationExamples)
                    {
                        Debug.LogWarning($"  - {example}");
                    }
                }
            }
            else
            {
                Debug.Log($"✓ All objects within depth limit");
            }

            if (report.childCountViolations > 0)
            {
                Debug.LogWarning($"⚠ CHILD COUNT VIOLATIONS: {report.childCountViolations} objects exceed child limit of {maxChildrenPerObject}");
                if (showDetailedReport && report.childCountViolationExamples.Count > 0)
                {
                    Debug.LogWarning("Examples of objects with too many children:");
                    foreach (string example in report.childCountViolationExamples)
                    {
                        Debug.LogWarning($"  - {example}");
                    }
                }
            }
            else
            {
                Debug.Log($"✓ All objects within child count limit");
            }

            if (showDetailedReport)
            {
                Debug.Log($"Deepest hierarchy: {report.deepestObjectPath}");
                Debug.Log($"Object with most children: {report.objectWithMostChildren}");
            }

            // Performance recommendations
            float estimatedActivationTime = EstimateActivationTime(report);
            Debug.Log($"Estimated Activation Time: ~{estimatedActivationTime:F2}ms");

            if (estimatedActivationTime > 16.67f) // 1 frame at 60 FPS
            {
                Debug.LogWarning($"⚠ Scene activation may cause frame hitches (>{estimatedActivationTime:F2}ms)");
                Debug.LogWarning("RECOMMENDATIONS:");
                Debug.LogWarning("  1. Flatten deep hierarchies (max 4 levels)");
                Debug.LogWarning("  2. Split objects with >50 children");
                Debug.LogWarning("  3. Use ScenePreloader to load scene in background");
                Debug.LogWarning("  4. Consider splitting scene into multiple subscenes");
            }

            Debug.Log($"=== END ANALYSIS ===\n");
        }

        /// <summary>
        /// Rough estimate of scene activation time based on object count and complexity
        /// Based on: ~0.001-0.005ms per GameObject during activation
        /// </summary>
        private float EstimateActivationTime(SceneAnalysisReport report)
        {
            // Base time: ~0.002ms per GameObject
            float baseTime = report.totalGameObjects * 0.002f;

            // Add penalty for depth violations (nested hierarchies are slower)
            float depthPenalty = report.depthViolations * 0.01f;

            // Add penalty for child count violations
            float childPenalty = report.childCountViolations * 0.005f;

            return baseTime + depthPenalty + childPenalty;
        }

        public static void AnalyzeSceneStatic(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.isLoaded)
            {
                SceneHierarchyAnalyzer analyzer = FindObjectOfType<SceneHierarchyAnalyzer>();
                if (analyzer == null)
                {
                    GameObject go = new GameObject("SceneHierarchyAnalyzer");
                    analyzer = go.AddComponent<SceneHierarchyAnalyzer>();
                }
                analyzer.AnalyzeScene(scene);
            }
        }
    }

    [System.Serializable]
    public class SceneAnalysisReport
    {
        public string sceneName;
        public int totalRootObjects;
        public int totalGameObjects;
        public int totalComponents;
        public int maxDepthFound;
        public string deepestObjectPath;
        public int maxChildrenFound;
        public string objectWithMostChildren;
        public int depthViolations;
        public int childCountViolations;
        public List<string> depthViolationExamples = new List<string>();
        public List<string> childCountViolationExamples = new List<string>();
    }
}
