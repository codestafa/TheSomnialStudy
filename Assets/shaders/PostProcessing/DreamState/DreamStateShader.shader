Shader "Custom/DreamStateShader"
{
    Properties
    {
        _Speed ("Speed", Float) = -10.0
        _Frequency ("Frequency", Float) = 20.0
        _ColorTint ("Color Tint", Color) = (0.8, 1.5, 3.0, 1.0)
        _Intensity ("Intensity", Range(0, 2)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "DreamStatePass"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Speed;
            float _Frequency;
            float4 _ColorTint;
            float _Intensity;

            void spin(inout float2 pos, float time)
            {
                float angle = time - atan2(length(pos), 1.0) * 3.0;
                float c = cos(angle);
                float s = sin(angle);
                pos = float2(pos.x * c - pos.y * s, pos.y * c + pos.x * s);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Sample the original screen texture
                float4 screenColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
                
                // Early return if intensity is near zero
                if (_Intensity < 0.001)
                    return screenColor;
                
                float time = _Time.y;
                float t = time * _Speed;
                
                float2 resolution = _ScreenParams.xy;
                float2 fragCoord = IN.texcoord * resolution;
                float2 position = (fragCoord - resolution * 0.5) / resolution.x;
                
                spin(position, time);
                
                float angle = atan2(position.y, position.x) / (2.0 * PI);
                angle -= floor(angle);
                float rad = max(length(position), 0.001);
                
                float angleRnd = floor(angle * 256.0) + 1.0;
                float angleRnd1 = frac(angleRnd * frac(angleRnd * 0.7235) * 45.1);
                float angleRnd2 = frac(angleRnd * frac(angleRnd * 0.82657) * 13.724);
                
                float t2 = t + angleRnd1 * _Frequency;
                float radDist = sqrt(angleRnd2 + 0.001);
                float adist = radDist / rad * 0.1;
                float dist = abs(frac(t2 * 0.1 + adist) - 0.5);
                dist = max(dist, 0.001);
                
                float outputColor = (1.0 / dist) * cos(0.7 * sin(t)) * adist / radDist / 30.0;
                
                float3 dreamColor = outputColor * _ColorTint.rgb;
                float3 finalColor = lerp(screenColor.rgb, dreamColor, _Intensity * 0.5);
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
