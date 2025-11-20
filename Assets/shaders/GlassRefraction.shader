Shader "Custom/GlassRefraction"
{
    Properties
    {
        _Color ("Glass Tint", Color) = (0.5, 0.8, 1, 0.1)
        _Thickness ("Thickness", Range(0, 1)) = 0.1
        _RefractionStrength ("Refraction Strength", Range(0, 1)) = 0.15
        _IndexOfRefraction ("Index of Refraction", Range(0.1, 2)) = 0.85
        _FresnelPower ("Fresnel Power", Range(0, 5)) = 1
        _Glossiness ("Smoothness", Range(0, 1)) = 0.95
        _CausticScale ("Caustic Scale", Float) = 30.0
        _CausticSpeed ("Caustic Speed", Float) = 2.0
        _CausticIntensity ("Caustic Intensity", Range(0, 2)) = 0.5
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        
        GrabPass { "_GrabTexture" }
        
        CGPROGRAM
        #pragma surface surf Standard alpha:fade vertex:vert
        #pragma target 3.0
        
        sampler2D _GrabTexture;
        float4 _GrabTexture_TexelSize;
        
        fixed4 _Color;
        float _Thickness;
        float _RefractionStrength;
        float _IndexOfRefraction;
        float _FresnelPower;
        float _Glossiness;
        float _CausticScale;
        float _CausticSpeed;
        float _CausticIntensity;
        
        struct Input
        {
            float4 screenPos;
            float3 worldPos;
            float3 worldNormal;
            float3 viewDir;
            INTERNAL_DATA
        };
        
        // Noise function for caustics
        float rand(float2 n)
        {
            return frac(sin(dot(n, float2(12.9898, 4.1414))) * 43758.5453);
        }
        
        float noise(float2 n)
        {
            float2 d = float2(0.0, 1.0);
            float2 b = floor(n);
            float2 f = smoothstep(0.0, 1.0, frac(n));
            return lerp(lerp(rand(b), rand(b + d.yx), f.x),
                       lerp(rand(b + d.xy), rand(b + d.yy), f.x), f.y);
        }
        
        // Caustic pattern
        float causticPattern(float3 worldPos)
        {
            float2 uv = worldPos.xz * _CausticScale;
            float time = _Time.y * _CausticSpeed;
            
            float n1 = noise(uv + time * 0.3);
            float n2 = noise(uv * 1.5 - time * 0.2);
            float n3 = noise(uv * 2.0 + time * 0.1);
            
            float caustic = (n1 + n2 * 0.5 + n3 * 0.25) / 1.75;
            caustic = pow(caustic, 3.0);
            
            return caustic * _CausticIntensity;
        }
        
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
        }
        
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Calculate refraction
            float3 worldNormal = WorldNormalVector(IN, o.Normal);
            float3 viewDir = normalize(IN.viewDir);
            
            // Fresnel effect
            float fresnel = pow(1.0 - saturate(dot(viewDir, worldNormal)), _FresnelPower);
            
            // Calculate refracted UV
            float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
            
            // Apply refraction offset
            float3 refractDir = refract(-viewDir, worldNormal, _IndexOfRefraction);
            float2 refractionOffset = refractDir.xy * _RefractionStrength * _Thickness;
            float2 refractedUV = screenUV + refractionOffset;
            
            // Sample the grabbed background
            fixed4 refractedColor = tex2D(_GrabTexture, refractedUV);
            
            // Add caustic effects
            float caustic = causticPattern(IN.worldPos);
            refractedColor.rgb += caustic * float3(1.0, 0.9, 0.6);
            
            // Tint with glass color
            refractedColor *= _Color;
            
            // Mix refraction with base color based on thickness
            float absorptionFactor = exp(-_Thickness * 5.0);
            refractedColor.rgb = lerp(_Color.rgb, refractedColor.rgb, absorptionFactor);
            
            // Output
            o.Albedo = refractedColor.rgb;
            o.Metallic = 0;
            o.Smoothness = _Glossiness;
            o.Alpha = saturate(_Color.a + fresnel * 0.3);
            o.Emission = caustic * 0.2 * float3(0.5, 0.8, 1.0);
        }
        ENDCG
    }
    
    FallBack "Transparent/Diffuse"
}
