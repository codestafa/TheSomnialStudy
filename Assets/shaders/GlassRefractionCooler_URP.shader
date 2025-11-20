Shader "URP/GlassRefractionCooler"
{
    Properties
    {
        [Header(Glass Properties)]
        _Color ("Glass Tint", Color) = (0.5, 0.8, 1, 0.3)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.95
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        
        [Header(Refraction)]
        _RefractionStrength ("Refraction Strength", Range(0, 0.5)) = 0.15
        _IOR ("Index of Refraction", Range(0.1, 2.0)) = 0.85
        _Thickness ("Thickness", Range(0, 2)) = 0.5
        _DistortionStrength ("Distortion Strength", Range(0, 1)) = 0.2
        
        [Header(Fresnel and Rim)]
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 3.0
        _FresnelStrength ("Fresnel Strength", Range(0, 2)) = 0.8
        _RimColor ("Rim Color", Color) = (0.8, 0.9, 1.0, 1.0)
        _RimPower ("Rim Power", Range(0.1, 8)) = 4.0
        _RimIntensity ("Rim Intensity", Range(0, 3)) = 1.0
        
        [Header(Caustics)]
        _CausticScale ("Caustic Scale", Float) = 20.0
        _CausticSpeed ("Caustic Speed", Float) = 1.0
        _CausticIntensity ("Caustic Intensity", Range(0, 3)) = 1.0
        _CausticContrast ("Caustic Contrast", Range(1, 5)) = 2.5
        _CausticColor ("Caustic Color", Color) = (1.0, 0.95, 0.7, 1.0)
        
        [Header(Rainbow Dispersion)]
        _ChromaticAberration ("Chromatic Aberration", Range(0, 0.1)) = 0.02
        _DispersionStrength ("Dispersion Rainbow", Range(0, 1)) = 0.3
        
        [Header(Iridescence)]
        _IridescenceStrength ("Iridescence Strength", Range(0, 2)) = 0.5
        _IridescenceScale ("Iridescence Scale", Range(0.1, 10)) = 2.0
        
        [Header(Animated Distortion)]
        _DistortionSpeed ("Distortion Speed", Range(0, 2)) = 0.5
        _DistortionScale ("Distortion Scale", Range(0, 50)) = 10.0
        
        [Header(Inner Glow)]
        _InnerGlow ("Inner Glow Intensity", Range(0, 2)) = 0.5
        _InnerGlowColor ("Inner Glow Color", Color) = (0.3, 0.6, 1.0, 1.0)
        
        [Header(Advanced)]
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 3)) = 1.0
        
        [Header(Sparkle)]
        _SparkleIntensity ("Sparkle Intensity", Range(0, 2)) = 0.3
        _SparkleScale ("Sparkle Scale", Range(10, 200)) = 50.0
        
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
                float _DistortionStrength;
                float _FresnelPower;
                float _FresnelStrength;
                float4 _RimColor;
                float _RimPower;
                float _RimIntensity;
                float _CausticScale;
                float _CausticSpeed;
                float _CausticIntensity;
                float _CausticContrast;
                float4 _CausticColor;
                float _ChromaticAberration;
                float _DispersionStrength;
                float _IridescenceStrength;
                float _IridescenceScale;
                float _DistortionSpeed;
                float _DistortionScale;
                float _InnerGlow;
                float4 _InnerGlowColor;
                float _NormalStrength;
                float _SparkleIntensity;
                float _SparkleScale;
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
            
            // Enhanced hash functions
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }
            
            float hash13(float3 p3)
            {
                p3 = frac(p3 * 0.1031);
                p3 += dot(p3, p3.zyx + 31.32);
                return frac((p3.x + p3.y) * p3.z);
            }
            
            // Smooth noise
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
            
            // 3D noise for extra effects
            float noise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                return lerp(
                    lerp(lerp(hash13(i), hash13(i + float3(1,0,0)), f.x),
                         lerp(hash13(i + float3(0,1,0)), hash13(i + float3(1,1,0)), f.x), f.y),
                    lerp(lerp(hash13(i + float3(0,0,1)), hash13(i + float3(1,0,1)), f.x),
                         lerp(hash13(i + float3(0,1,1)), hash13(i + float3(1,1,1)), f.x), f.y),
                    f.z);
            }
            
            // Fractal Brownian Motion
            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for(int i = 0; i < 4; i++)
                {
                    value += amplitude * noise(p * frequency);
                    frequency *= 2.1;
                    amplitude *= 0.45;
                }
                return value;
            }
            
            // Enhanced caustic pattern
            float causticPattern(float3 worldPos, float time)
            {
                float2 uv = worldPos.xz * _CausticScale;
                
                float c = 0.0;
                
                // Layer 1: Primary caustics
                float2 offset1 = float2(sin(time * 0.5) * 0.3, cos(time * 0.3) * 0.4);
                c += fbm(uv + offset1);
                
                // Layer 2: Secondary caustics
                float2 offset2 = float2(cos(time * 0.7) * 0.2, sin(time * 0.4) * 0.3);
                c += fbm(uv * 1.5 - offset2) * 0.5;
                
                // Layer 3: Detail
                c += noise(uv * 3.0 + time * 0.2) * 0.3;
                
                // Enhance contrast
                c = pow(saturate(c * 0.4), _CausticContrast);
                
                return c * _CausticIntensity;
            }
            
            // Iridescence effect
            float3 iridescence(float3 viewDir, float3 normal)
            {
                float NdotV = saturate(dot(normal, viewDir));
                float iridescent = pow(1.0 - NdotV, _IridescenceScale);
                
                float3 iriColor;
                iriColor.r = sin(iridescent * 3.14159 * 2.0) * 0.5 + 0.5;
                iriColor.g = sin(iridescent * 3.14159 * 2.0 + 2.094) * 0.5 + 0.5;
                iriColor.b = sin(iridescent * 3.14159 * 2.0 + 4.189) * 0.5 + 0.5;
                
                return iriColor * _IridescenceStrength;
            }
            
            // Sparkle effect
            float sparkle(float3 worldPos, float3 normal, float3 viewDir)
            {
                float3 sparklePos = worldPos * _SparkleScale;
                float sparkleNoise = hash13(floor(sparklePos));
                
                float3 reflected = reflect(-viewDir, normal);
                float sparkleAngle = saturate(dot(reflected, float3(0, 1, 0)));
                
                float sparkle = step(0.98, sparkleNoise) * pow(sparkleAngle, 10.0);
                return sparkle * _SparkleIntensity;
            }
            
            // Animated distortion
            float2 getDistortion(float3 worldPos, float time)
            {
                float2 distortion;
                distortion.x = sin(worldPos.y * _DistortionScale + time * _DistortionSpeed) * 0.5;
                distortion.y = cos(worldPos.x * _DistortionScale + time * _DistortionSpeed * 0.7) * 0.5;
                return distortion * _DistortionStrength * 0.01;
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
                float time = _Time.y * _CausticSpeed;
                
                // Normalize vectors
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                
                // Sample and apply normal map
                float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv), _NormalStrength);
                float3 bitangentWS = cross(normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                normalWS = normalize(normalTS.x * input.tangentWS.xyz + normalTS.y * bitangentWS + normalTS.z * normalWS);
                
                // Calculate fresnel
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, _FresnelPower) * _FresnelStrength;
                
                // Calculate refraction
                float3 refractDir = refract(-viewDirWS, normalWS, _IOR);
                
                // Screen space UV
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                // Add animated distortion
                float2 animDistortion = getDistortion(input.positionWS, time);
                
                // Enhanced chromatic aberration with dispersion
                float2 baseDistortion = refractDir.xy * _RefractionStrength + animDistortion;
                
                half3 refractedColor;
                float aberration = _ChromaticAberration * (1.0 + fresnel * _DispersionStrength);
                
                // Sample RGB channels separately for chromatic aberration
                refractedColor.r = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, 
                    screenUV + baseDistortion * (1.0 + aberration * 2.0)).r;
                refractedColor.g = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, 
                    screenUV + baseDistortion).g;
                refractedColor.b = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, 
                    screenUV + baseDistortion * (1.0 - aberration * 2.0)).b;
                
                // Enhanced caustics
                float caustic = causticPattern(input.positionWS, time);
                half3 causticColor = caustic * _CausticColor.rgb;
                
                // Add pulsing to caustics
                float causticPulse = sin(time * 2.0) * 0.1 + 0.9;
                causticColor *= causticPulse;
                
                refractedColor += causticColor;
                
                // Apply glass color and absorption
                float absorption = exp(-_Thickness * 2.0);
                refractedColor = lerp(_Color.rgb, refractedColor, absorption);
                refractedColor *= _Color.rgb;
                
                // Add iridescence
                float3 iriColor = iridescence(viewDirWS, normalWS);
                refractedColor += iriColor;
                
                // Inner glow effect
                float innerGlow = pow(1.0 - NdotV, 6.0) * _InnerGlow;
                refractedColor += _InnerGlowColor.rgb * innerGlow;
                
                // Get main light for specular
                Light mainLight = GetMainLight();
                
                // Enhanced specular with multiple highlights
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specular = pow(NdotH, _Smoothness * 256.0) * _Smoothness;
                
                // Add secondary specular lobe for more complexity
                float specular2 = pow(NdotH, _Smoothness * 64.0) * _Smoothness * 0.3;
                
                refractedColor += (specular + specular2) * mainLight.color;
                
                // Add sparkles
                float sparkleEffect = sparkle(input.positionWS, normalWS, viewDirWS);
                refractedColor += sparkleEffect * float3(1, 1, 1);
                
                // Enhanced rim lighting with color
                float rim = pow(1.0 - NdotV, _RimPower);
                float3 rimLight = rim * _RimColor.rgb * _RimIntensity;
                
                // Add animated rim pulse
                rimLight *= sin(time * 1.5) * 0.2 + 0.8;
                
                refractedColor += rimLight;
                
                // Depth-based color gradient
                float depthFade = saturate((input.positionCS.z / input.positionCS.w) * 0.5);
                refractedColor = lerp(refractedColor, refractedColor * _Color.rgb * 1.2, depthFade * 0.3);
                
                // Final alpha with enhanced fresnel
                half alpha = _Color.a + fresnel * 0.5;
                alpha = saturate(alpha);
                
                // Apply fog
                refractedColor = MixFog(refractedColor, input.fogFactor);
                
                return half4(refractedColor, alpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}
