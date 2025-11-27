using UnityEngine;
using System.Collections;

/// <summary>
/// Runtime controller for the Retro Pixel shader effect.
/// Allows dynamic adjustment of shader properties and preset switching.
/// </summary>
public class RetroPixelController : MonoBehaviour
{
    [Header("Material Reference")]
    [SerializeField] private Material retroPixelMaterial;
    
    [Header("Preset Settings")]
    [SerializeField] private RetroPreset currentPreset = RetroPreset.Balanced;
    [SerializeField] private bool applyPresetOnStart = true;
    
    [Header("Dynamic Effects")]
    [SerializeField] private bool animateScanlines = false;
    [SerializeField] private float scanlineAnimationSpeed = 1f;
    
    [SerializeField] private bool pulseEffect = false;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private Vector2 pixelSizeRange = new Vector2(2f, 4f);
    
    [Header("Input Controls")]
    [SerializeField] private KeyCode increasePixelationKey = KeyCode.PageUp;
    [SerializeField] private KeyCode decreasePixelationKey = KeyCode.PageDown;
    [SerializeField] private KeyCode toggleEffectKey = KeyCode.F9;
    [SerializeField] private KeyCode cyclePresetKey = KeyCode.F10;
    
    private bool effectEnabled = true;
    private float basePixelSize;
    private float baseScanlineIntensity;
    
    // Shader property IDs for performance
    private static readonly int PixelSizeID = Shader.PropertyToID("_PixelSize");
    private static readonly int ScanlineIntensityID = Shader.PropertyToID("_ScanlineIntensity");
    private static readonly int ScanlineSpeedID = Shader.PropertyToID("_ScanlineSpeed");
    private static readonly int VignetteStrengthID = Shader.PropertyToID("_VignetteStrength");
    private static readonly int ColorDepthID = Shader.PropertyToID("_ColorDepth");
    private static readonly int NoiseAmountID = Shader.PropertyToID("_NoiseAmount");
    private static readonly int BrightnessID = Shader.PropertyToID("_Brightness");
    private static readonly int ContrastID = Shader.PropertyToID("_Contrast");
    private static readonly int ChromaticAberrationID = Shader.PropertyToID("_ChromaticAberration");
    private static readonly int CRTCurvatureID = Shader.PropertyToID("_CRTCurvature");
    private static readonly int ColorBleedingID = Shader.PropertyToID("_ColorBleeding");
    private static readonly int PhosphorGlowID = Shader.PropertyToID("_PhosphorGlow");
    private static readonly int ScreenDoorEffectID = Shader.PropertyToID("_ScreenDoorEffect");
    
    public enum RetroPreset
    {
        Subtle,
        Balanced,
        ClassicCRT,
        HeavyPixel,
        Custom
    }
    
    [System.Serializable]
    public class PresetData
    {
        public float pixelSize = 2f;
        public float scanlineIntensity = 0.1f;
        public float vignetteStrength = 0.1f;
        public float colorDepth = 24f;
        public float noiseAmount = 0.015f;
        public float brightness = 1.05f;
        public float contrast = 1f;
        public float chromaticAberration = 0.001f;
        public float crtCurvature = 0.05f;
        public float colorBleeding = 0.02f;
        public float phosphorGlow = 0.3f;
        public float screenDoorEffect = 0.1f;
    }
    
    private void Start()
    {
        if (retroPixelMaterial == null)
        {
            Debug.LogError("RetroPixelController: No material assigned!");
            enabled = false;
            return;
        }
        
        // Store base values
        basePixelSize = retroPixelMaterial.GetFloat(PixelSizeID);
        baseScanlineIntensity = retroPixelMaterial.GetFloat(ScanlineIntensityID);
        
        if (applyPresetOnStart)
        {
            ApplyPreset(currentPreset);
        }
    }
    
    private void Update()
    {
        HandleInput();
        
        if (!effectEnabled)
            return;
        
        // Dynamic scanline animation
        if (animateScanlines)
        {
            float scanlineSpeed = Mathf.Sin(Time.time * scanlineAnimationSpeed) * 5f + 5f;
            retroPixelMaterial.SetFloat(ScanlineSpeedID, scanlineSpeed);
        }
        
        // Pulse effect
        if (pulseEffect)
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
            float pixelSize = Mathf.Lerp(pixelSizeRange.x, pixelSizeRange.y, pulse);
            retroPixelMaterial.SetFloat(PixelSizeID, pixelSize);
        }
    }
    
    private void HandleInput()
    {
        // Toggle effect on/off
        if (Input.GetKeyDown(toggleEffectKey))
        {
            ToggleEffect();
        }
        
        // Increase pixelation
        if (Input.GetKeyDown(increasePixelationKey))
        {
            AdjustPixelSize(1f);
        }
        
        // Decrease pixelation
        if (Input.GetKeyDown(decreasePixelationKey))
        {
            AdjustPixelSize(-1f);
        }
        
        // Cycle through presets
        if (Input.GetKeyDown(cyclePresetKey))
        {
            CyclePreset();
        }
    }
    
    public void ToggleEffect()
    {
        effectEnabled = !effectEnabled;
        
        if (effectEnabled)
        {
            ApplyPreset(currentPreset);
        }
        else
        {
            DisableEffect();
        }
    }
    
    public void DisableEffect()
    {
        retroPixelMaterial.SetFloat(PixelSizeID, 1f);
        retroPixelMaterial.SetFloat(ScanlineIntensityID, 0f);
        retroPixelMaterial.SetFloat(VignetteStrengthID, 0f);
        retroPixelMaterial.SetFloat(NoiseAmountID, 0f);
        retroPixelMaterial.SetFloat(ChromaticAberrationID, 0f);
        retroPixelMaterial.SetFloat(CRTCurvatureID, 0f);
        retroPixelMaterial.SetFloat(PhosphorGlowID, 0f);
        retroPixelMaterial.SetFloat(ScreenDoorEffectID, 0f);
    }
    
    public void AdjustPixelSize(float delta)
    {
        float currentSize = retroPixelMaterial.GetFloat(PixelSizeID);
        float newSize = Mathf.Clamp(currentSize + delta, 1f, 8f);
        retroPixelMaterial.SetFloat(PixelSizeID, newSize);
        Debug.Log($"Pixel Size: {newSize}");
    }
    
    public void CyclePreset()
    {
        int nextPreset = ((int)currentPreset + 1) % System.Enum.GetValues(typeof(RetroPreset)).Length;
        currentPreset = (RetroPreset)nextPreset;
        ApplyPreset(currentPreset);
        Debug.Log($"Preset: {currentPreset}");
    }
    
    public void ApplyPreset(RetroPreset preset)
    {
        PresetData data = GetPresetData(preset);
        ApplyPresetData(data);
        currentPreset = preset;
    }
    
    private PresetData GetPresetData(RetroPreset preset)
    {
        switch (preset)
        {
            case RetroPreset.Subtle:
                return new PresetData
                {
                    pixelSize = 2f,
                    scanlineIntensity = 0.05f,
                    vignetteStrength = 0.05f,
                    colorDepth = 24f,
                    noiseAmount = 0.01f,
                    brightness = 1.02f,
                    contrast = 1.0f,
                    chromaticAberration = 0.0005f,
                    crtCurvature = 0.02f,
                    colorBleeding = 0.01f,
                    phosphorGlow = 0.2f,
                    screenDoorEffect = 0.05f
                };
                
            case RetroPreset.Balanced:
                return new PresetData
                {
                    pixelSize = 3f,
                    scanlineIntensity = 0.1f,
                    vignetteStrength = 0.1f,
                    colorDepth = 24f,
                    noiseAmount = 0.015f,
                    brightness = 1.05f,
                    contrast = 1.0f,
                    chromaticAberration = 0.001f,
                    crtCurvature = 0.05f,
                    colorBleeding = 0.02f,
                    phosphorGlow = 0.3f,
                    screenDoorEffect = 0.1f
                };
                
            case RetroPreset.ClassicCRT:
                return new PresetData
                {
                    pixelSize = 3f,
                    scanlineIntensity = 0.2f,
                    vignetteStrength = 0.15f,
                    colorDepth = 16f,
                    noiseAmount = 0.02f,
                    brightness = 1.08f,
                    contrast = 1.1f,
                    chromaticAberration = 0.002f,
                    crtCurvature = 0.08f,
                    colorBleeding = 0.03f,
                    phosphorGlow = 0.4f,
                    screenDoorEffect = 0.15f
                };
                
            case RetroPreset.HeavyPixel:
                return new PresetData
                {
                    pixelSize = 6f,
                    scanlineIntensity = 0.15f,
                    vignetteStrength = 0.12f,
                    colorDepth = 12f,
                    noiseAmount = 0.025f,
                    brightness = 1.1f,
                    contrast = 1.15f,
                    chromaticAberration = 0.003f,
                    crtCurvature = 0.06f,
                    colorBleeding = 0.04f,
                    phosphorGlow = 0.35f,
                    screenDoorEffect = 0.2f
                };
                
            default: // Custom - return current values
                return new PresetData
                {
                    pixelSize = retroPixelMaterial.GetFloat(PixelSizeID),
                    scanlineIntensity = retroPixelMaterial.GetFloat(ScanlineIntensityID),
                    vignetteStrength = retroPixelMaterial.GetFloat(VignetteStrengthID),
                    colorDepth = retroPixelMaterial.GetFloat(ColorDepthID),
                    noiseAmount = retroPixelMaterial.GetFloat(NoiseAmountID),
                    brightness = retroPixelMaterial.GetFloat(BrightnessID),
                    contrast = retroPixelMaterial.GetFloat(ContrastID),
                    chromaticAberration = retroPixelMaterial.GetFloat(ChromaticAberrationID),
                    crtCurvature = retroPixelMaterial.GetFloat(CRTCurvatureID),
                    colorBleeding = retroPixelMaterial.GetFloat(ColorBleedingID),
                    phosphorGlow = retroPixelMaterial.GetFloat(PhosphorGlowID),
                    screenDoorEffect = retroPixelMaterial.GetFloat(ScreenDoorEffectID)
                };
        }
    }
    
    private void ApplyPresetData(PresetData data)
    {
        retroPixelMaterial.SetFloat(PixelSizeID, data.pixelSize);
        retroPixelMaterial.SetFloat(ScanlineIntensityID, data.scanlineIntensity);
        retroPixelMaterial.SetFloat(VignetteStrengthID, data.vignetteStrength);
        retroPixelMaterial.SetFloat(ColorDepthID, data.colorDepth);
        retroPixelMaterial.SetFloat(NoiseAmountID, data.noiseAmount);
        retroPixelMaterial.SetFloat(BrightnessID, data.brightness);
        retroPixelMaterial.SetFloat(ContrastID, data.contrast);
        retroPixelMaterial.SetFloat(ChromaticAberrationID, data.chromaticAberration);
        retroPixelMaterial.SetFloat(CRTCurvatureID, data.crtCurvature);
        retroPixelMaterial.SetFloat(ColorBleedingID, data.colorBleeding);
        retroPixelMaterial.SetFloat(PhosphorGlowID, data.phosphorGlow);
        retroPixelMaterial.SetFloat(ScreenDoorEffectID, data.screenDoorEffect);
    }
    
    /// <summary>
    /// Smoothly transition to a new preset over time
    /// </summary>
    public void TransitionToPreset(RetroPreset preset, float duration)
    {
        StartCoroutine(TransitionCoroutine(preset, duration));
    }
    
    private IEnumerator TransitionCoroutine(RetroPreset targetPreset, float duration)
    {
        PresetData startData = GetPresetData(RetroPreset.Custom); // Current values
        PresetData endData = GetPresetData(targetPreset);
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            PresetData lerpData = new PresetData
            {
                pixelSize = Mathf.Lerp(startData.pixelSize, endData.pixelSize, t),
                scanlineIntensity = Mathf.Lerp(startData.scanlineIntensity, endData.scanlineIntensity, t),
                vignetteStrength = Mathf.Lerp(startData.vignetteStrength, endData.vignetteStrength, t),
                colorDepth = Mathf.Lerp(startData.colorDepth, endData.colorDepth, t),
                noiseAmount = Mathf.Lerp(startData.noiseAmount, endData.noiseAmount, t),
                brightness = Mathf.Lerp(startData.brightness, endData.brightness, t),
                contrast = Mathf.Lerp(startData.contrast, endData.contrast, t),
                chromaticAberration = Mathf.Lerp(startData.chromaticAberration, endData.chromaticAberration, t),
                crtCurvature = Mathf.Lerp(startData.crtCurvature, endData.crtCurvature, t),
                colorBleeding = Mathf.Lerp(startData.colorBleeding, endData.colorBleeding, t),
                phosphorGlow = Mathf.Lerp(startData.phosphorGlow, endData.phosphorGlow, t),
                screenDoorEffect = Mathf.Lerp(startData.screenDoorEffect, endData.screenDoorEffect, t)
            };
            
            ApplyPresetData(lerpData);
            yield return null;
        }
        
        ApplyPresetData(endData);
        currentPreset = targetPreset;
    }
    
    // Public API for runtime control
    public void SetPixelSize(float size) => retroPixelMaterial.SetFloat(PixelSizeID, Mathf.Clamp(size, 1f, 8f));
    public void SetScanlineIntensity(float intensity) => retroPixelMaterial.SetFloat(ScanlineIntensityID, Mathf.Clamp01(intensity));
    public void SetCRTCurvature(float curvature) => retroPixelMaterial.SetFloat(CRTCurvatureID, Mathf.Clamp(curvature, 0f, 0.2f));
    public void SetPhosphorGlow(float glow) => retroPixelMaterial.SetFloat(PhosphorGlowID, Mathf.Clamp01(glow));
    public void SetScreenDoorEffect(float effect) => retroPixelMaterial.SetFloat(ScreenDoorEffectID, Mathf.Clamp01(effect));
    public void SetColorDepth(float depth) => retroPixelMaterial.SetFloat(ColorDepthID, Mathf.Clamp(depth, 4f, 32f));
    public void SetBrightness(float brightness) => retroPixelMaterial.SetFloat(BrightnessID, Mathf.Clamp(brightness, 0.8f, 1.2f));
}
