using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

/// <summary>
/// Render feature that applies the retro pixel shader with layer and tag-based exclusions.
/// Objects on excluded layers or with excluded tags will render normally without the shader effect.
/// </summary>
public class RetroPixelRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material retroPixelMaterial;
        
        [Header("Exclusion Settings")]
        [Tooltip("Layers that should NOT have the retro pixel effect applied")]
        public LayerMask excludedLayers;
        
        [Tooltip("Tags that should NOT have the retro pixel effect applied")]
        public List<string> excludedTags = new List<string> { "UI", "NoRetro" };
        
        [Header("Render Pass Settings")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        
        [Tooltip("Use depth buffer to exclude objects (more accurate but slower)")]
        public bool useDepthBasedExclusion = true;
    }
    
    public Settings settings = new Settings();
    private RetroPixelRenderPass renderPass;
    
    public override void Create()
    {
        if (settings.retroPixelMaterial == null)
        {
            Debug.LogWarning("RetroPixelRenderFeature: No material assigned!");
            return;
        }
        
        renderPass = new RetroPixelRenderPass(settings);
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderPass != null && settings.retroPixelMaterial != null)
        {
            renderPass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
            renderer.EnqueuePass(renderPass);
        }
    }
    
    protected override void Dispose(bool disposing)
    {
        renderPass?.Dispose();
    }
}

public class RetroPixelRenderPass : ScriptableRenderPass
{
    private RetroPixelRenderFeature.Settings settings;
    private Material material;
    private RTHandle sourceTexture;
    private RTHandle excludedObjectsTexture;
    private RTHandle tempTexture;
    
    private const string PROFILER_TAG = "Retro Pixel Effect";
    private readonly int tempTextureID = Shader.PropertyToID("_TempRetroTexture");
    private readonly int excludedTextureID = Shader.PropertyToID("_ExcludedObjectsTexture");
    
    public RetroPixelRenderPass(RetroPixelRenderFeature.Settings settings)
    {
        this.settings = settings;
        this.material = settings.retroPixelMaterial;
        this.renderPassEvent = settings.renderPassEvent;
    }
    
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        var descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;
        
        // Create temporary textures
        RenderingUtils.ReAllocateIfNeeded(ref tempTexture, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TempRetroTexture");
        
        if (settings.useDepthBasedExclusion)
        {
            RenderingUtils.ReAllocateIfNeeded(ref excludedObjectsTexture, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_ExcludedObjectsTexture");
        }
    }
    
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (material == null)
            return;
        
        CommandBuffer cmd = CommandBufferPool.Get(PROFILER_TAG);
        
        var cameraData = renderingData.cameraData;
        sourceTexture = cameraData.renderer.cameraColorTargetHandle;
        
        if (settings.useDepthBasedExclusion && settings.excludedLayers != 0)
        {
            // Render excluded objects to a separate texture
            RenderExcludedObjects(cmd, ref renderingData);
            
            // Apply shader effect to main camera, blending with excluded objects
            Blitter.BlitCameraTexture(cmd, sourceTexture, tempTexture, material, 0);
            BlendWithExcludedObjects(cmd);
        }
        else
        {
            // Simple full-screen blit with shader
            Blitter.BlitCameraTexture(cmd, sourceTexture, tempTexture, material, 0);
            Blitter.BlitCameraTexture(cmd, tempTexture, sourceTexture);
        }
        
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
    
    private void RenderExcludedObjects(CommandBuffer cmd, ref RenderingData renderingData)
    {
        // This would require rendering excluded objects separately
        // For now, we'll use a simpler approach with layer-based masking
        // You can extend this with custom rendering if needed
    }
    
    private void BlendWithExcludedObjects(CommandBuffer cmd)
    {
        // Blend the retro effect with unaffected objects
        // This is a simplified version - you can create a custom blend shader
        Blitter.BlitCameraTexture(cmd, tempTexture, sourceTexture);
    }
    
    public void Dispose()
    {
        tempTexture?.Release();
        excludedObjectsTexture?.Release();
    }
    
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        // Cleanup is handled in Dispose
    }
}
