Shader "Custom/BlackHoleEffect"
{
    Properties
    {
        _EffectAmount ("Effect Amount", Range(0,1)) = 0
        _ZoomSpeed ("Zoom Speed", Float) = 1.0
        _DistortStrength ("Distortion Strength", Range(0,0.05)) = 0.01
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _NoiseIntensity ("Noise Intensity", Range(0,1)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _EffectAmount;
            float _ZoomSpeed;
            float _DistortStrength;
            float _NoiseIntensity;
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.texcoord;

                // ==== 1. Frozen screen shrinking ====
                float zoom = 1.0 - _EffectAmount * _ZoomSpeed;
                float2 centeredUV = (uv - 0.5) / zoom + 0.5;

                float3 col = 0;
                if (all(centeredUV >= 0) && all(centeredUV <= 1))
                {
                    col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, centeredUV).rgb;
                }

                // ==== 2. Void distortion (soft waves) ====
                float2 warp = uv;
                warp += sin((uv.yx + _Time.y * 0.2) * 6.0) * _DistortStrength * _EffectAmount;
                float vignette = smoothstep(0.9, 0.3, length(uv - 0.5));
                col *= vignette;

                // ==== 3. Soft Noise Fog ====
                float2 noiseUV = uv * 3.0 + _Time.y * 0.05;
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                col += noise * _NoiseIntensity * _EffectAmount * 0.2;

                // ==== 4. Radial Ripple Glow ====
                float r = length(uv - 0.5);
                float ripple = sin(r * 30.0 - _Time.y * 3.0) * 0.5 + 0.5;
                ripple *= smoothstep(0.5, 0.0, r); // only near center
                col += ripple * 0.1 * _EffectAmount;

                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }
}