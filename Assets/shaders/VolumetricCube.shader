Shader "Custom/VolumetricCube"
{
    Properties
    {
        _BackfaceTex("Backface Texture", 2D) = "white" {}
        _NoiseTex3D("Noise Texture", 3D) = "" {}
        _Density("Density", Range(0,2)) = 1.0
        _StepSize("Step Size", Range(0.01, 1.0)) = 0.05
        _Color("Fog Color", Color) = (0.6,0.7,1,1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back   // only front faces, so we raymarch inside

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
                float4 screenPos  : TEXCOORD1;
            };

            // Textures
            TEXTURE2D(_BackfaceTex);
            SAMPLER(sampler_BackfaceTex);

            TEXTURE3D(_NoiseTex3D);
            SAMPLER(sampler_NoiseTex3D);

            float _Density;
            float _StepSize;
            float4 _Color;

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldPos   = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.screenPos  = ComputeScreenPos(OUT.positionCS); // gives us screen UVs
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                // ✅ Use screen UVs (normalized 0-1)
                float2 uv = IN.screenPos.xy / IN.screenPos.w;

                // read backface world position from RT
                float3 exitPos = SAMPLE_TEXTURE2D(_BackfaceTex, sampler_BackfaceTex, uv).rgb;
                float3 entryPos = IN.worldPos;

                // ray setup
                float3 rayDir = exitPos - entryPos;
                float rayLen = length(rayDir);
                rayDir /= rayLen;

                float3 pos = entryPos;
                float step = _StepSize;
                int steps = min(128, (int)(rayLen / step));

                float3 col = 0;
                float transmittance = 1.0;

                [loop]
                for (int i = 0; i < 128; i++)
                {
                    if (i >= steps || transmittance < 0.01) break;

                    // convert world pos to object local uvw
                    float3 localPos = mul(UNITY_MATRIX_I_M, float4(pos,1)).xyz;
                    float3 uvw = localPos + 0.5; // from [-0.5,0.5] → [0,1]

                    float density = SAMPLE_TEXTURE3D(_NoiseTex3D, sampler_NoiseTex3D, uvw).r * _Density;

                    col += transmittance * _Color.rgb * density * step;
                    transmittance *= exp(-density * step);

                    pos += rayDir * step;
                }

                return float4(col, 1.0 - transmittance);
            }
            ENDHLSL
        }
    }
}
