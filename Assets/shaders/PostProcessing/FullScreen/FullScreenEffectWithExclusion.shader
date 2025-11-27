Shader "Custom/FullScreenEffectWithExclusion"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        
        // Add your effect properties here
        _EffectStrength ("Effect Strength", Range(0, 1)) = 1.0
        _Pixelation ("Pixelation", Range(1, 256)) = 128
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
        }
        
        // Pass 0: Main effect pass - only renders where stencil != 1
        Pass
        {
            Name "FullScreenEffect"
            
            ZTest Always
            ZWrite Off
            Cull Off
            
            // Stencil test: only render where stencil is NOT 1 (not excluded)
            Stencil
            {
                Ref 1
                Comp NotEqual
                Pass Keep
            }
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            
            float _EffectStrength;
            float _Pixelation;
            
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                
                // ========================================
                // YOUR EFFECT CODE HERE
                // Example: Pixelation effect
                // ========================================
                
                float2 pixelatedUV = floor(uv * _Pixelation) / _Pixelation;
                half4 pixelatedColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, pixelatedUV);
                half4 originalColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                
                // Lerp between original and effect based on strength
                half4 finalColor = lerp(originalColor, pixelatedColor, _EffectStrength);
                
                return finalColor;
            }
            ENDHLSL
        }
        
        // Pass 1: Copy excluded pixels pass - only renders where stencil == 1
        Pass
        {
            Name "CopyExcluded"
            
            ZTest Always
            ZWrite Off
            Cull Off
            
            // Stencil test: only render where stencil IS 1 (excluded objects)
            Stencil
            {
                Ref 1
                Comp Equal
                Pass Keep
            }
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            half4 Frag(Varyings input) : SV_Target
            {
                // Just copy original pixels for excluded areas
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord);
            }
            ENDHLSL
        }
    }
    
    Fallback Off
}
