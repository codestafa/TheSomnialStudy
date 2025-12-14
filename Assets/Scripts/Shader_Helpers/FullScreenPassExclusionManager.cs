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
    private bool hasScannedScene = false;

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
        // Don't scan on enable - let objects self-register instead
        // This prevents 17K+ object scan during scene preload
    }

    private void OnDisable()
    {
        // Don't clear - objects should persist across scene loads
        // excludedObjects.Clear();
    }

    /// <summary>
    /// Lazy-load all excluded objects (only called when needed as fallback)
    /// </summary>
    private void EnsureScanned()
    {
        if (hasScannedScene)
            return;

        // Only scan once as fallback if objects haven't self-registered
        ExcludeFromFullScreenPass[] allExcluders = FindObjectsByType<ExcludeFromFullScreenPass>(FindObjectsSortMode.None);
        foreach (var excluder in allExcluders)
        {
            if (excluder != null && !excludedObjects.Contains(excluder))
                excludedObjects.Add(excluder);
        }
        hasScannedScene = true;
    }

    /// <summary>
    /// Check if any object is excluded from full screen passes
    /// </summary>
    public static bool IsExcluded(Renderer renderer)
    {
        if (instance == null || renderer == null)
            return false;

        // Lazy-load on first use
        instance.EnsureScanned();

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

        // Lazy-load on first use
        instance.EnsureScanned();

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