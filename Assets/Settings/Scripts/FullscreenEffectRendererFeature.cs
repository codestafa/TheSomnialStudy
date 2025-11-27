using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DreamStateRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;
        public Material passMaterial;

        [Header("Effect Parameters")]
        [Range(0f, 2f)]
        public float intensity = 0f;
        public float speed = -10f;
        public float frequency = 20f;
        public Color colorTint = new Color(0.8f, 1.5f, 3f, 1f);
    }

    public Settings settings = new Settings();
    private DreamStateRenderPass dreamPass;
    private bool requiresColor = true;

    // Property IDs
    private static readonly int SpeedId = Shader.PropertyToID("_Speed");
    private static readonly int FrequencyId = Shader.PropertyToID("_Frequency");
    private static readonly int ColorTintId = Shader.PropertyToID("_ColorTint");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

    public override void Create()
    {
        dreamPass = new DreamStateRenderPass();
        dreamPass.renderPassEvent = settings.injectionPoint;

        // Configure input requirements
        ScriptableRenderPassInput modifiedRequirements = ScriptableRenderPassInput.Color;

        // Remove Color flag to avoid unnecessary CopyColor pass (unless before transparents)
        if (settings.injectionPoint > RenderPassEvent.BeforeRenderingTransparents)
        {
            modifiedRequirements ^= ScriptableRenderPassInput.Color;
        }

        dreamPass.ConfigureInput(modifiedRequirements);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.passMaterial == null)
        {
            Debug.LogWarning("DreamStateRendererFeature: Missing material. Pass will not execute.");
            return;
        }

        // Update material properties before rendering
        UpdateMaterialProperties();

        dreamPass.Setup(settings.passMaterial, requiresColor, renderingData);
        renderer.EnqueuePass(dreamPass);
    }

    private void UpdateMaterialProperties()
    {
        if (settings.passMaterial == null)
            return;

        settings.passMaterial.SetFloat(SpeedId, settings.speed);
        settings.passMaterial.SetFloat(FrequencyId, settings.frequency);
        settings.passMaterial.SetColor(ColorTintId, settings.colorTint);
        settings.passMaterial.SetFloat(IntensityId, settings.intensity);
    }

    protected override void Dispose(bool disposing)
    {
        dreamPass?.Dispose();
    }

    // Public methods for runtime control
    public void SetIntensity(float value)
    {
        settings.intensity = Mathf.Clamp(value, 0f, 2f);
        UpdateMaterialProperties();
    }

    public void SetSpeed(float value)
    {
        settings.speed = value;
        UpdateMaterialProperties();
    }

    public void SetFrequency(float value)
    {
        settings.frequency = value;
        UpdateMaterialProperties();
    }

    public void SetColorTint(Color color)
    {
        settings.colorTint = color;
        UpdateMaterialProperties();
    }

    class DreamStateRenderPass : ScriptableRenderPass
    {
        private Material passMaterial;
        private bool requiresColor;
        private RTHandle copiedColor;
        private new ProfilingSampler profilingSampler;
        private static readonly int BlitTextureShaderID = Shader.PropertyToID("_BlitTexture");

        public DreamStateRenderPass()
        {
            profilingSampler = new ProfilingSampler("DreamStateEffect");
        }

        public void Setup(Material mat, bool requiresColor, in RenderingData renderingData)
        {
            this.passMaterial = mat;
            this.requiresColor = requiresColor;

            if (requiresColor)
            {
                var colorCopyDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                colorCopyDescriptor.depthBufferBits = 0;
                RenderingUtils.ReAllocateHandleIfNeeded(ref copiedColor, colorCopyDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_DreamStateColorCopy");
            }
        }

        public void Dispose()
        {
            copiedColor?.Release();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (passMaterial == null)
                return;

            ref var cameraData = ref renderingData.cameraData;

            // Skip preview cameras
            if (cameraData.isPreviewCamera)
                return;

            // Check intensity and skip if too low (optimization)
            float intensity = passMaterial.GetFloat("_Intensity");
            if (intensity < 0.001f)
                return;

            CommandBuffer cmd = CommandBufferPool.Get("DreamStateEffect");

            using (new ProfilingScope(cmd, profilingSampler))
            {
                RTHandle cameraTarget = cameraData.renderer.cameraColorTargetHandle;

                if (requiresColor)
                {
                    // Copy camera color to our temp texture
                    Blitter.BlitCameraTexture(cmd, cameraTarget, copiedColor);

                    // Set it as the source texture for the material
                    passMaterial.SetTexture(BlitTextureShaderID, copiedColor);
                }

                // Draw full screen effect
                Blitter.BlitCameraTexture(cmd, cameraTarget, cameraTarget, passMaterial, 0);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}