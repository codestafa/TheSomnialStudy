using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RetroPixelRenderFeatureWorking : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Header("Material")]
        public Material material;
        [Header("Render Settings")]
        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingTransparents;
        [Header("Performance")]
        [Range(0, 2)]
        [Tooltip("Downsample: 0=none, 1=half res, 2=quarter res")]
        public int downsample = 0;
    }
    public Settings settings = new Settings();
    private RetroPixelPass pass;

    public override void Create()
    {
        pass = new RetroPixelPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.material == null)
        {
            Debug.LogWarning("RetroPixelRenderFeature: Material is null!");
            return;
        }
        if (renderingData.cameraData.cameraType == CameraType.SceneView)
            return;

        pass.Setup(renderer, settings.material);
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && pass != null)
        {
            pass.Dispose();
        }
    }
}

public class RetroPixelPass : ScriptableRenderPass
{
    private Material material;
    private RTHandle copiedColor;
    private int downsample;
    private const string ProfilerTag = "Retro Pixel Effect";
    private static readonly int BlitTextureID = Shader.PropertyToID("_BlitTexture");

    public RetroPixelPass(RetroPixelRenderFeatureWorking.Settings settings)
    {
        material = settings.material;
        renderPassEvent = settings.passEvent;
        downsample = settings.downsample;

        ConfigureInput(ScriptableRenderPassInput.Color);
    }

    public void Setup(ScriptableRenderer renderer, Material mat)
    {
        material = mat;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;

        if (downsample > 0)
        {
            descriptor.width >>= downsample;
            descriptor.height >>= downsample;
        }

        RenderingUtils.ReAllocateHandleIfNeeded(
            ref copiedColor,
            descriptor,
            FilterMode.Point,
            TextureWrapMode.Clamp,
            name: "_RetroPixelColorCopy"
        );
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (material == null)
            return;

        CommandBuffer cmd = CommandBufferPool.Get(ProfilerTag);

        try
        {
            using (new ProfilingScope(cmd, new ProfilingSampler(ProfilerTag)))
            {
                // Get the source color texture
                RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;

                // Copy the color buffer to our temporary texture
                Blitter.BlitCameraTexture(cmd, source, copiedColor);

                // Set the copied color as a shader property
                material.SetTexture(BlitTextureID, copiedColor);

                // Blit back from our effect to the source (displays on screen)
                Blitter.BlitCameraTexture(cmd, copiedColor, source, material, 0);
            }
        }
        finally
        {
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    public void Dispose()
    {
        copiedColor?.Release();
    }
}