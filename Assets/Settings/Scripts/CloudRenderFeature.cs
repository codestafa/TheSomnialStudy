using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CloudRenderFeature : ScriptableRendererFeature
{
    class CloudRenderPass : ScriptableRenderPass
    {
        private readonly string profilerTag = "Cloud Render Pass";
        private readonly Material material;

        private RTHandle tempRT;
        private RTHandle source;

        public CloudRenderPass(Material mat)
        {
            material = mat;
        }

        public void Setup(RTHandle src)
        {
            source = src;
        }

        [System.Obsolete]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;

            RenderingUtils.ReAllocateHandleIfNeeded(
                ref tempRT, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CloudTemp");
        }

        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null || source == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

            // main blit: apply the clouds to camera color
            Blitter.BlitCameraTexture(cmd, source, tempRT, material, 0);
            Blitter.BlitCameraTexture(cmd, tempRT, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            tempRT?.Release();
        }
    }

    [SerializeField] Shader shader;
    Material material;
    CloudRenderPass pass;

    public override void Create()
    {
        if (shader != null)
            material = CoreUtils.CreateEngineMaterial(shader);

        pass = new CloudRenderPass(material)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null)
            return;

        // ✅ this is the safe call site; renderer.cameraColorTargetHandle is valid here
        pass.Setup(renderer.cameraColorTargetHandle);
        renderer.EnqueuePass(pass);
    }
}
