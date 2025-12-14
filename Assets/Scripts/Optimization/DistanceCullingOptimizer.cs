using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Runtime distance-based culling for heavy background objects.
/// Disables renderers and objects beyond a certain distance from the camera.
/// </summary>
public class DistanceCullingOptimizer : MonoBehaviour
{
    [Header("Culling Settings")]
    [SerializeField] private float cullDistance = 150f;
    [SerializeField] private float hysteresis = 10f; // Prevents popping
    [SerializeField] private float updateInterval = 0.25f;

    [Header("Auto-Detection")]
    [SerializeField] private bool autoDetectHeavyObjects = true;
    [SerializeField] private int triangleThreshold = 10000;
    [SerializeField] private string[] heavyObjectKeywords = { "Sea", "Lake", "Ocean", "Water", "Background", "Distant" };

    [Header("Manual Targets")]
    [SerializeField] private List<GameObject> additionalTargets = new List<GameObject>();

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private Camera mainCamera;
    private float updateTimer;
    private List<CullableObject> cullableObjects = new List<CullableObject>();

    private class CullableObject
    {
        public GameObject gameObject;
        public Renderer[] renderers;
        public Collider[] colliders;
        public Vector3 center;
        public float boundingRadius;
        public bool isCulled;
        public int triangleCount;
    }

    private void Start()
    {
        mainCamera = Camera.main;
        if (autoDetectHeavyObjects)
        {
            DetectHeavyObjects();
        }
        RegisterAdditionalTargets();

        if (showDebugInfo)
        {
            Debug.Log($"[DistanceCulling] Registered {cullableObjects.Count} cullable objects");
        }
    }

    private void DetectHeavyObjects()
    {
        Renderer[] allRenderers = FindObjectsOfType<Renderer>(true);

        foreach (var renderer in allRenderers)
        {
            if (renderer == null) continue;

            // Check by keyword
            bool matchesKeyword = false;
            string objName = renderer.gameObject.name.ToLower();
            string parentName = renderer.transform.parent != null ? renderer.transform.parent.name.ToLower() : "";

            foreach (string keyword in heavyObjectKeywords)
            {
                if (objName.Contains(keyword.ToLower()) || parentName.Contains(keyword.ToLower()))
                {
                    matchesKeyword = true;
                    break;
                }
            }

            // Check by triangle count
            int triangles = GetTriangleCount(renderer);
            bool isHeavy = triangles >= triangleThreshold;

            if (matchesKeyword || isHeavy)
            {
                RegisterObject(renderer.gameObject, triangles);
            }
        }
    }

    private int GetTriangleCount(Renderer renderer)
    {
        MeshFilter mf = renderer.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            try
            {
                return (int)(mf.sharedMesh.GetIndexCount(0) / 3);
            }
            catch
            {
                return mf.sharedMesh.vertexCount / 3;
            }
        }

        SkinnedMeshRenderer smr = renderer as SkinnedMeshRenderer;
        if (smr != null && smr.sharedMesh != null)
        {
            try
            {
                return (int)(smr.sharedMesh.GetIndexCount(0) / 3);
            }
            catch
            {
                return smr.sharedMesh.vertexCount / 3;
            }
        }

        return 0;
    }

    private void RegisterAdditionalTargets()
    {
        foreach (var target in additionalTargets)
        {
            if (target != null)
            {
                RegisterObject(target, 0);
            }
        }
    }

    private void RegisterObject(GameObject obj, int triangles)
    {
        // Check if already registered
        foreach (var existing in cullableObjects)
        {
            if (existing.gameObject == obj) return;
        }

        var cullable = new CullableObject
        {
            gameObject = obj,
            renderers = obj.GetComponentsInChildren<Renderer>(true),
            colliders = obj.GetComponentsInChildren<Collider>(true),
            isCulled = false,
            triangleCount = triangles
        };

        // Calculate bounding info
        if (cullable.renderers.Length > 0)
        {
            Bounds combinedBounds = cullable.renderers[0].bounds;
            foreach (var r in cullable.renderers)
            {
                combinedBounds.Encapsulate(r.bounds);
            }
            cullable.center = combinedBounds.center;
            cullable.boundingRadius = combinedBounds.extents.magnitude;
        }
        else
        {
            cullable.center = obj.transform.position;
            cullable.boundingRadius = 5f;
        }

        cullableObjects.Add(cullable);

        if (showDebugInfo)
        {
            Debug.Log($"[DistanceCulling] Registered: {obj.name} ({triangles} tris, radius: {cullable.boundingRadius:F1})");
        }
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            return;
        }

        updateTimer += Time.deltaTime;
        if (updateTimer < updateInterval) return;
        updateTimer = 0f;

        Vector3 camPos = mainCamera.transform.position;

        foreach (var cullable in cullableObjects)
        {
            if (cullable.gameObject == null) continue;

            float distance = Vector3.Distance(camPos, cullable.center);

            if (cullable.isCulled)
            {
                // Object is culled - check if should become visible
                if (distance < cullDistance - hysteresis)
                {
                    SetCulled(cullable, false);
                }
            }
            else
            {
                // Object is visible - check if should be culled
                if (distance > cullDistance + hysteresis)
                {
                    SetCulled(cullable, true);
                }
            }
        }
    }

    private void SetCulled(CullableObject cullable, bool culled)
    {
        cullable.isCulled = culled;

        // Disable renderers (cheaper than disabling GameObjects)
        foreach (var renderer in cullable.renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = !culled;
            }
        }

        // Optionally disable colliders for culled objects
        foreach (var collider in cullable.colliders)
        {
            if (collider != null)
            {
                collider.enabled = !culled;
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"[DistanceCulling] {cullable.gameObject.name} {(culled ? "CULLED" : "VISIBLE")}");
        }
    }

    /// <summary>
    /// Manually register an object for culling.
    /// </summary>
    public void RegisterForCulling(GameObject obj)
    {
        if (obj != null)
        {
            RegisterObject(obj, GetTriangleCount(obj.GetComponent<Renderer>()));
        }
    }

    /// <summary>
    /// Get current culling statistics.
    /// </summary>
    public (int total, int culled, int trianglesSaved) GetStats()
    {
        int culled = 0;
        int trianglesSaved = 0;

        foreach (var c in cullableObjects)
        {
            if (c.isCulled)
            {
                culled++;
                trianglesSaved += c.triangleCount;
            }
        }

        return (cullableObjects.Count, culled, trianglesSaved);
    }

    private void OnDrawGizmosSelected()
    {
        if (mainCamera == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(mainCamera.transform.position, cullDistance);

        foreach (var cullable in cullableObjects)
        {
            if (cullable.gameObject == null) continue;

            Gizmos.color = cullable.isCulled ? Color.red : Color.green;
            Gizmos.DrawWireSphere(cullable.center, cullable.boundingRadius);
        }
    }
}
