Shader "Custom/VolumetricCube_Backface"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Opaque" }
        Cull Front   // render backfaces

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.worldPos = TransformObjectToWorld(v.positionOS.xyz);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return float4(i.worldPos, 1.0); // store exit point in RGB
            }
            ENDHLSL
        }
    }
}
