Shader "BlackHole/Schwarzschild"
{
    Properties
    {
        [Header(Spacetime)]
        _c ("Speed Of Light", Float) = 1.0

        [Header(Black Hole)]
        _a ("Schwarzschild Radius", Float) = 1.0

        [Header(Ring)]
        _RingRadius ("Ring Radius", Float) = 8.0
        [NoScaleOffset] _RedshiftTex ("Redshift Texture", 2D) = "white" {}

        [Header(Bloom)]
        _sigma ("sigma", Float) = 1.25
        _StepWidth ("Step Width", Float) = 8.0
        _Threshold ("Threshold", Float) = 0.8
        _Suppression ("Suppression", Float) = 0.7
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        CGINCLUDE
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define PI 3.14159265359

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _a;
            float _RingRadius;
            float _c;

            sampler2D _RedshiftTex;
            float4 _RedshiftTex_ST;

            // Simple distortion function
            float2 DistortUV(float2 uv, float2 center, float radius, float strength)
            {
                float2 dir = uv - center;
                float dist = length(dir);
                float distort = strength / max(dist, 0.001);
                return center + normalize(dir) * dist * (1.0 + distort * exp(-dist * radius));
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = (v.vertex.xy + 1.0) * 0.5; // map to 0–1
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 center = float2(0.5, 0.5);
                float2 distortedUV = DistortUV(i.uv, center, _RingRadius, _a);

                fixed4 col = tex2D(_RedshiftTex, distortedUV);

                // darken near the event horizon
                float d = distance(i.uv, center);
                if (d < 0.2 * _a) col = 0;

                return col;
            }
        ENDCG

        Pass { }
    }
}
