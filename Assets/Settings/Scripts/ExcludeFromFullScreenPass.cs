using UnityEngine;

/// <summary>
/// Add this component to any GameObject to exclude it from full screen effects.
/// The object will be moved to the exclusion layer (31 by default) which the 
/// StencilWriterRendererFeature uses to mark pixels in the stencil buffer.
/// </summary>
[DisallowMultipleComponent]
public class ExcludeFromFullScreenPass : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The layer used for exclusion. Must match the layer in StencilWriterRendererFeature settings.")]
    private int exclusionLayer = 31;
    
    [SerializeField]
    [Tooltip("If true, exclusion is active. Disable to temporarily include this object in effects.")]
    private bool isExcluded = true;
    
    private int originalLayer;
    private bool wasExcluded;

    private void OnEnable()
    {
        originalLayer = gameObject.layer;
        wasExcluded = isExcluded;

        if (isExcluded)
        {
            gameObject.layer = exclusionLayer;
        }

        // Self-register with manager to avoid expensive FindObjectsByType scan
        if (FullScreenPassExclusionManager.Instance != null)
        {
            FullScreenPassExclusionManager.Instance.RegisterExcluder(this);
        }
    }

    private void OnDisable()
    {
        // Restore original layer when disabled
        gameObject.layer = originalLayer;

        // Unregister from manager
        if (FullScreenPassExclusionManager.Instance != null)
        {
            FullScreenPassExclusionManager.Instance.UnregisterExcluder(this);
        }
    }

    private void Update()
    {
        // Handle runtime changes to isExcluded
        if (wasExcluded != isExcluded)
        {
            wasExcluded = isExcluded;
            gameObject.layer = isExcluded ? exclusionLayer : originalLayer;
        }
    }

    /// <summary>
    /// Enable or disable exclusion at runtime
    /// </summary>
    public void SetExcluded(bool excluded)
    {
        isExcluded = excluded;
    }

    /// <summary>
    /// Check if this object is currently excluded
    /// </summary>
    public bool IsExcluded => isExcluded;

    private void OnValidate()
    {
        // Clamp exclusion layer to valid range
        exclusionLayer = Mathf.Clamp(exclusionLayer, 0, 31);
    }
}
