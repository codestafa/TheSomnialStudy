using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Simple controller that assigns/removes a material to/from Unity's Full Screen Pass Renderer Feature
/// Use with Timeline's Activation Track to enable/disable the effect
/// </summary>
public class DreamEffectToggleController : MonoBehaviour
{
    [Header("Renderer Setup")]
    [Tooltip("Your URP Renderer Asset (e.g., UniversalRenderer, ForwardRenderer)")]
    [SerializeField] private UniversalRendererData rendererData;

    [Tooltip("Index of the Full Screen Pass Renderer Feature (usually 0 if it's the first one)")]
    [SerializeField] private int rendererFeatureIndex = 0;

    [Header("Effect Material")]
    [Tooltip("The dream state material to activate")]
    [SerializeField] private Material dreamMaterial;

    [Header("Effect Parameters")]
    [Range(0f, 2f)]
    [SerializeField] private float intensity = 1f;
    [SerializeField] private float speed = -10f;
    [SerializeField] private float frequency = 20f;
    [SerializeField] private Color colorTint = new Color(0.8f, 1.5f, 3f, 1f);

    private Material originalMaterial;
    private FullScreenPassRendererFeature fullScreenFeature;

    // Property IDs
    private static readonly int SpeedId = Shader.PropertyToID("_Speed");
    private static readonly int FrequencyId = Shader.PropertyToID("_Frequency");
    private static readonly int ColorTintId = Shader.PropertyToID("_ColorTint");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

    private void Awake()
    {
        // Get reference to the Full Screen Pass Renderer Feature
        if (rendererData != null && rendererData.rendererFeatures.Count > rendererFeatureIndex)
        {
            fullScreenFeature = rendererData.rendererFeatures[rendererFeatureIndex] as FullScreenPassRendererFeature;

            if (fullScreenFeature != null)
            {
                // Store the original material
                originalMaterial = fullScreenFeature.passMaterial;
            }
            else
            {
                Debug.LogError("DreamEffectToggleController: Renderer feature at index " + rendererFeatureIndex + " is not a FullScreenPassRendererFeature!");
            }
        }
        else
        {
            Debug.LogError("DreamEffectToggleController: Invalid renderer data or feature index!");
        }
    }

    private void OnEnable()
    {
        // Activate the effect when enabled
        ActivateEffect();
    }

    private void OnDisable()
    {
        // Deactivate the effect when disabled
        DeactivateEffect();
    }

    private void Update()
    {
        // Update material properties in real-time
        if (dreamMaterial != null && enabled)
        {
            UpdateMaterialProperties();
        }
    }

    private void ActivateEffect()
    {
        if (fullScreenFeature != null && dreamMaterial != null)
        {
            fullScreenFeature.passMaterial = dreamMaterial;
            UpdateMaterialProperties();
            Debug.Log("DreamEffectToggleController: Effect activated");
        }
    }

    private void DeactivateEffect()
    {
        if (fullScreenFeature != null)
        {
            fullScreenFeature.passMaterial = originalMaterial;
            Debug.Log("DreamEffectToggleController: Effect deactivated");
        }
    }

    private void UpdateMaterialProperties()
    {
        if (dreamMaterial == null)
            return;

        dreamMaterial.SetFloat(SpeedId, speed);
        dreamMaterial.SetFloat(FrequencyId, frequency);
        dreamMaterial.SetColor(ColorTintId, colorTint);
        dreamMaterial.SetFloat(IntensityId, intensity);
    }

    // Public methods for external control
    public void SetIntensity(float value)
    {
        intensity = Mathf.Clamp(value, 0f, 2f);
        UpdateMaterialProperties();
    }

    public void SetSpeed(float value)
    {
        speed = value;
        UpdateMaterialProperties();
    }

    public void SetFrequency(float value)
    {
        frequency = value;
        UpdateMaterialProperties();
    }

    public void SetColorTint(Color color)
    {
        colorTint = color;
        UpdateMaterialProperties();
    }
}