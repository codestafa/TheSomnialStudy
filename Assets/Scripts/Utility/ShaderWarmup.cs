using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Warms up shaders at scene start to prevent compilation stutters during gameplay.
/// Attach this to a GameObject in scenes with complex shaders (water, post-processing, etc.)
/// </summary>
public class ShaderWarmup : MonoBehaviour
{
    [Header("Warmup Settings")]
    [Tooltip("Optional: Specific ShaderVariantCollection to warm up")]
    public ShaderVariantCollection shaderVariants;

    [Tooltip("Warm up all shaders in the project (slower but more thorough)")]
    public bool warmupAllShaders = false;

    [Tooltip("Materials to force-render for warmup (drag complex materials here)")]
    public Material[] materialsToWarmup;

    [Header("Debug")]
    public bool logWarmupTime = true;

    private void Awake()
    {
        WarmupShaders();
    }

    private void WarmupShaders()
    {
        float startTime = Time.realtimeSinceStartup;

        // Method 1: Warm up specific shader variant collection
        if (shaderVariants != null)
        {
            shaderVariants.WarmUp();
            if (logWarmupTime)
                Debug.Log($"[ShaderWarmup] ShaderVariantCollection warmed up");
        }

        // Method 2: Warm up all shaders (use sparingly - can be slow)
        if (warmupAllShaders)
        {
            Shader.WarmupAllShaders();
            if (logWarmupTime)
                Debug.Log($"[ShaderWarmup] All shaders warmed up");
        }

        // Method 3: Force materials to compile by accessing their shader
        if (materialsToWarmup != null)
        {
            foreach (var mat in materialsToWarmup)
            {
                if (mat != null && mat.shader != null)
                {
                    // Force shader compilation by accessing pass count
                    int passCount = mat.passCount;

                    // Also force keyword processing
                    var keywords = mat.shaderKeywords;
                }
            }
            if (logWarmupTime)
                Debug.Log($"[ShaderWarmup] {materialsToWarmup.Length} materials warmed up");
        }

        if (logWarmupTime)
        {
            float elapsed = (Time.realtimeSinceStartup - startTime) * 1000f;
            Debug.Log($"[ShaderWarmup] Total warmup time: {elapsed:F2}ms");
        }
    }
}
