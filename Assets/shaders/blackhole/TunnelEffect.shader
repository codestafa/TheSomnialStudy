Shader "Custom/TunnelEffect"
{
    Properties
    {
        _Speed ("Tunnel Speed", Float) = 0.5
        _Scale ("Tunnel Scale", Float) = 0.5
        _Mode ("Shape Mode (0=circle,1=square)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Speed;
            float _Scale;
            float _Mode;
            static const float kPi = 3.1415927;

            half4 Frag(Varyings IN) : SV_Target
            {
                // get normalized coords (-1..1)
                float2 p = (2.0 * IN.texcoord - 1.0);
                p.x *= _ScreenParams.x / _ScreenParams.y; // aspect correction

                float a = atan2(p.y, p.x);
                float r;
                if (_Mode < 0.5) {
                    r = length(p); // circular tunnel
                } else {
                    float2 p2 = p * p;
                    float2 p4 = p2 * p2;
                    float2 p8 = p4 * p4;
                    r = pow(p8.x + p8.y, 1.0 / 8.0); // square tunnel
                }

                // polar UV
                float2 uv = float2(_Scale / r + _Time.y * _Speed, a / kPi);

                // Procedural stripes (no texture required)
                float stripe = step(0.5, frac(uv.x));
                float3 col = lerp(float3(0.0, 0.1, 0.3), float3(1.0, 0.8, 0.3), stripe);

                col *= r; // darken near center
                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
