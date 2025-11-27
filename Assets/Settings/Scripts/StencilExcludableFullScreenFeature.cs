using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

/// <summary>
/// Combined renderer feature that handles stencil writing AND the full screen effect.
/// This is simpler to set up than using two separate features.
/// </summary>
public class StencilExcludableFullScreenFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Header("Effect Settings")]
        [Tooltip("The material for your full screen effect")]
        public Material effectMaterial;

        [Tooltip("When to apply the full screen effect")]
        public RenderPassEvent effectRenderEvent = RenderPassEvent.AfterRenderingPostProcessing;

        [Header("Exclusion Settings")]
        [Tooltip("Layer mask for objects to exclude from the effect")]
        public LayerMask excludedLayers = 1 << 31;

        [Tooltip("Stencil value used to mark excluded pixels")]
        [Range(1, 255)]
        public int stencilValue = 1;
    }

    public Settings settings = new Settings();

    private StencilWritePass stencilWritePass;
    private FullScreenEffectPass effectPass;
    private Material stencilWriterMaterial;

    public override void Create()
    {
        var stencilShader = Shader.Find("Hidden/StencilWriter");
        if (stencilShader != null)
        {
            stencilWriterMaterial = CoreUtils.CreateEngineMaterial(stencilShader);
        }
        else
        {
            Debug.LogError("StencilExcludableFullScreenFeature: Cannot find Hidden/StencilWriter shader!");
        }

        stencilWritePass = new StencilWritePass(settings, stencilWriterMaterial);
        effectPass = new FullScreenEffectPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.effectMaterial == null || stencilWriterMaterial == null)
            return;

        if (renderingData.cameraData.cameraType == CameraType.Preview)
            return;

        renderer.EnqueuePass(stencilWritePass);
        renderer.EnqueuePass(effectPass);
    }

    protected override void Dispose(bool disposing)
    {
        if (stencilWriterMaterial != null)
        {
            CoreUtils.Destroy(stencilWriterMaterial);
            stencilWriterMaterial = null;
        }
    }

    // ==========================================
    // STENCIL WRITE PASS - Uses RendererList API
    // ==========================================
    class StencilWritePass : ScriptableRenderPass
    {
        private Settings settings;
        private Material material;
        private FilteringSettings filteringSettings;

        private static readonly ShaderTagId[] shaderTagIds = new ShaderTagId[]
        {
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit")
        };

        public StencilWritePass(Settings settings, Material material)
        {
            this.settings = settings;
            this.material = material;
            renderPassEvent = settings.effectRenderEvent - 1;
            filteringSettings = new FilteringSettings(RenderQueueRange.all, settings.excludedLayers);
        }

        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();

            if (!resourceData.activeDepthTexture.IsValid())
                return;

            // Setup drawing settings
            var sortingSettings = new SortingSettings(cameraData.camera)
            {
                criteria = SortingCriteria.CommonOpaque
            };

            var drawingSettings = new DrawingSettings(shaderTagIds[0], sortingSettings)
            {
                overrideMaterial = material,
                overrideMaterialPassIndex = 0,
                enableDynamicBatching = true,
                enableInstancing = true
            };

            for (int i = 1; i < shaderTagIds.Length; i++)
            {
                drawingSettings.SetShaderPassName(i, shaderTagIds[i]);
            }

            // Create renderer list - this is the Unity 6 Render Graph way
            var rendererListParams = new RendererListParams(
                renderingData.cullResults,
                drawingSettings,
                filteringSettings
            );
            var rendererListHandle = renderGraph.CreateRendererList(rendererListParams);

            using (var builder = renderGraph.AddRasterRenderPass<StencilPassData>("Write Stencil For Exclusion", out var passData))
            {
                passData.rendererListHandle = rendererListHandle;

                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);
                builder.UseRendererList(rendererListHandle);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((StencilPassData data, RasterGraphContext ctx) =>
                {
                    ctx.cmd.DrawRendererList(data.rendererListHandle);
                });
            }
        }

        private class StencilPassData
        {
            public RendererListHandle rendererListHandle;
        }
    }

    // ==========================================
    // FULL SCREEN EFFECT PASS
    // ==========================================
    class FullScreenEffectPass : ScriptableRenderPass
    {
        private Settings settings;

        public FullScreenEffectPass(Settings settings)
        {
            this.settings = settings;
            renderPassEvent = settings.effectRenderEvent;
        }

        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();

            if (resourceData.isActiveTargetBackBuffer)
                return;

            var source = resourceData.activeColorTexture;

            // Create temp texture
            var destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name = "_TempEffectTexture";
            destinationDesc.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

            // Pass 1: Apply effect (skips stencil == 1 pixels)
            using (var builder = renderGraph.AddRasterRenderPass<EffectPassData>("Apply Full Screen Effect", out var passData))
            {
                passData.source = source;
                passData.material = settings.effectMaterial;

                builder.SetRenderAttachment(destination, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                builder.UseTexture(source);

                builder.SetRenderFunc((EffectPassData data, RasterGraphContext ctx) =>
                {
                    data.material.SetTexture("_MainTex", data.source);
                    Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            // Pass 2: Copy excluded pixels (only stencil == 1 pixels)
            using (var builder = renderGraph.AddRasterRenderPass<EffectPassData>("Copy Excluded Pixels", out var passData))
            {
                passData.source = source;
                passData.material = settings.effectMaterial;

                builder.SetRenderAttachment(destination, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                builder.UseTexture(source);

                builder.SetRenderFunc((EffectPassData data, RasterGraphContext ctx) =>
                {
                    data.material.SetTexture("_MainTex", data.source);
                    Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 1);
                });
            }

            // Pass 3: Copy result back to source
            using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("Copy Back Result", out var passData))
            {
                passData.source = destination;

                builder.SetRenderAttachment(source, 0);
                builder.UseTexture(destination);

                builder.SetRenderFunc((CopyPassData data, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1, 1, 0, 0), 0, false);
                });
            }
        }

        private class EffectPassData
        {
            public TextureHandle source;
            public Material material;
        }

        private class CopyPassData
        {
            public TextureHandle source;
        }
    }
}