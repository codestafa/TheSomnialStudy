Shader "URP/GlassRefraction"
{
    Properties
    {
        [Header(Glass Properties)]
        _Color ("Glass Tint", Color) = (0.5, 0.8, 1, 0.3)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.95
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        
        [Header(Refraction)]
        _RefractionStrength ("Refraction Strength", Range(0, 0.5)) = 0.1
        _IOR ("Index of Refraction", Range(0.1, 2.0)) = 0.85
        _Thickness ("Thickness", Range(0, 2)) = 0.5
        
        [Header(Fresnel)]
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 3.0
        _FresnelStrength ("Fresnel Strength", Range(0, 1)) = 0.5
        
        [Header(Caustics)]
        _CausticScale ("Caustic Scale", Float) = 20.0
        _CausticSpeed ("Caustic Speed", Float) = 1.0
        _CausticIntensity ("Caustic Intensity", Range(0, 2)) = 0.5
        
        [Header(Advanced)]
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 2)) = 1.0
        _ChromaticAberration ("Chromatic Aberration", Range(0, 0.05)) = 0.01
        
        [HideInInspector] _Surface("__surface", Float) = 1.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _Cull("__cull", Float) = 2.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline"
        }
        
        LOD 300
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);
            
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Smoothness;
                float _Metallic;
                float _RefractionStrength;
                float _IOR;
                float _Thickness;
                float _FresnelPower;
                float _FresnelStrength;
                float _CausticScale;
                float _CausticSpeed;
                float _CausticIntensity;
                float _NormalStrength;
                float _ChromaticAberration;
                float4 _NormalMap_ST;
            CBUFFER_END
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
                float3 viewDirWS : TEXCOORD5;
                float fogFactor : TEXCOORD6;
            };
            
            // Hash function for noise
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            
            // Noise function
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            // Caustic pattern
            float causticPattern(float3 worldPos, float time)
            {
                float2 uv = worldPos.xz * _CausticScale;
                
                float c = 0.0;
                float freq = 1.0;
                float amp = 1.0;
                
                for (int i = 0; i < 3; i++)
                {
                    float2 offset = float2(time * 0.3, -time * 0.2) * (i + 1) * 0.5;
                    c += noise(uv * freq + offset) * amp;
                    freq *= 2.1;
                    amp *= 0.45;
                }
                
                c = pow(saturate(c * 0.5), 2.5);
                return c * _CausticIntensity;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.tangentWS = float4(normalInput.tangentWS, input.tangentOS.w);
                output.uv = TRANSFORM_TEX(input.uv, _NormalMap);
                output.screenPos = ComputeScreenPos(vertexInput.positionCS);
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Normalize vectors
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                
                // Sample normal map
                float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv), _NormalStrength);
                float3 bitangentWS = cross(normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                normalWS = normalize(normalTS.x * input.tangentWS.xyz + normalTS.y * bitangentWS + normalTS.z * normalWS);
                
                // Calculate fresnel
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, _FresnelPower) * _FresnelStrength;
                
                // Calculate refraction
                float3 refractDir = refract(-viewDirWS, normalWS, _IOR);
                
                // Screen space UV for background sampling
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                // Apply distortion with chromatic aberration
                float2 distortion = refractDir.xy * _RefractionStrength;
                
                half3 refractedColor;
                refractedColor.r = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, 
                    screenUV + distortion * (1.0 + _ChromaticAberration)).r;
                refractedColor.g = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, 
                    screenUV + distortion).g;
                refractedColor.b = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, 
                    screenUV + distortion * (1.0 - _ChromaticAberration)).b;
                
                // Add caustic effects
                float time = _Time.y * _CausticSpeed;
                float caustic = causticPattern(input.positionWS, time);
                
                // Apply caustic lighting
                half3 causticColor = caustic * half3(1.0, 0.95, 0.7);
                refractedColor += causticColor;
                
                // Apply glass color and absorption
                float absorption = exp(-_Thickness * 2.0);
                refractedColor = lerp(_Color.rgb, refractedColor, absorption);
                refractedColor *= _Color.rgb;
                
                // Add specular highlights
                Light mainLight = GetMainLight();
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specular = pow(NdotH, _Smoothness * 128.0) * _Smoothness;
                refractedColor += specular * mainLight.color * 0.5;
                
                // Add rim lighting
                float rim = pow(1.0 - NdotV, 3.0);
                refractedColor += rim * _Color.rgb * 0.1;
                
                // Final color with fresnel
                half3 finalColor = refractedColor;
                half alpha = _Color.a + fresnel * 0.3;
                
                // Apply fog
                finalColor = MixFog(finalColor, input.fogFactor);
                
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}
