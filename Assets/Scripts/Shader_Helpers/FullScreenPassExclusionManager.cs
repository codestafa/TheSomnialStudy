using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manager for tracking objects excluded from full screen passes.
/// Add this to your scene (once) - it will automatically register/unregister objects.
/// </summary>
public class FullScreenPassExclusionManager : MonoBehaviour
{
    private static FullScreenPassExclusionManager instance;
    public static FullScreenPassExclusionManager Instance => instance;

    private List<ExcludeFromFullScreenPass> excludedObjects = new List<ExcludeFromFullScreenPass>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // Register all existing excluded objects (Unity 6 API)
        ExcludeFromFullScreenPass[] allExcluders = FindObjectsByType<ExcludeFromFullScreenPass>(FindObjectsSortMode.None);
        foreach (var excluder in allExcluders)
        {
            if (!excludedObjects.Contains(excluder))
                excludedObjects.Add(excluder);
        }
    }

    private void OnDisable()
    {
        excludedObjects.Clear();
    }

    /// <summary>
    /// Check if any object is excluded from full screen passes
    /// </summary>
    public static bool IsExcluded(Renderer renderer)
    {
        if (instance == null || renderer == null)
            return false;

        var excluder = renderer.GetComponent<ExcludeFromFullScreenPass>();
        return excluder != null && instance.excludedObjects.Contains(excluder);
    }

    /// <summary>
    /// Get all renderers that should be excluded from full screen passes
    /// </summary>
    public static List<Renderer> GetExcludedRenderers()
    {
        List<Renderer> excluded = new List<Renderer>();

        if (instance == null)
            return excluded;

        foreach (var excluder in instance.excludedObjects)
        {
            if (excluder != null)
            {
                Renderer renderer = excluder.GetComponent<Renderer>();
                if (renderer != null)
                    excluded.Add(renderer);
            }
        }
        return excluded;
    }

    public void RegisterExcluder(ExcludeFromFullScreenPass excluder)
    {
        if (!excludedObjects.Contains(excluder))
            excludedObjects.Add(excluder);
    }

    public void UnregisterExcluder(ExcludeFromFullScreenPass excluder)
    {
        excludedObjects.Remove(excluder);
    }
}