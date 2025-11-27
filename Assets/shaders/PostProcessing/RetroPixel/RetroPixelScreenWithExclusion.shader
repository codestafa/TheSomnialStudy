Shader "Custom/RetroPixelScreenWithExclusion"
{
    Properties
    {
        _PixelSize ("Pixel Size", Range(1, 8)) = 2
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.15
        _ScanlineSpeed ("Scanline Speed", Float) = 5.0
        _VignetteStrength ("Vignette Strength", Range(0, 1)) = 0.25
        _ColorDepth ("Color Depth", Range(4, 32)) = 24
        _NoiseAmount ("Noise Amount", Range(0, 0.1)) = 0.01
        _Brightness ("Brightness", Range(0.5, 1.5)) = 1.0
        _Contrast ("Contrast", Range(0.5, 2.0)) = 1.1
        _ChromaticAberration ("Chromatic Aberration", Range(0, 0.01)) = 0.003
        
        _CRTCurvature ("CRT Curvature", Range(0, 0.3)) = 0.08
        _ColorBleeding ("Color Bleeding", Range(0, 0.2)) = 0.05
        _PhosphorGlow ("Phosphor Glow", Range(0, 1)) = 0.4
        _ScreenDoorEffect ("Screen Door Effect", Range(0, 1)) = 0.15
        _GlassReflection ("Glass Reflection", Range(0, 0.5)) = 0.1
        _WarmthShift ("Warmth Shift", Range(0, 1)) = 0.15
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        
        Pass
        {
            Name "RetroEffect"
            
            ZWrite Off 
            Cull Off 
            ZTest Always
            
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
            
            // Constants
            static const float SUBPIXEL_RED_THRESHOLD = 0.33;
            static const float SUBPIXEL_BLUE_THRESHOLD = 0.66;
            static const float VIGNETTE_MULTIPLIER = 15.0;
            static const float EDGE_FADE_THRESHOLD = 0.01;
            static const float PHOSPHOR_SAMPLE_COUNT = 3.0;
            
            float _PixelSize;
            float _ScanlineIntensity;
            float _ScanlineSpeed;
            float _VignetteStrength;
            float _ColorDepth;
            float _NoiseAmount;
            float _Brightness;
            float _Contrast;
            float _ChromaticAberration;
            float _CRTCurvature;
            float _ColorBleeding;
            float _PhosphorGlow;
            float _ScreenDoorEffect;
            float _GlassReflection;
            float _WarmthShift;
            
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            float2 CRTCurve(float2 uv)
            {
                uv = uv * 2.0 - 1.0;
                float2 offset = abs(uv.yx) * _CRTCurvature;
                uv = uv + uv * offset * offset;
                uv = uv * 0.5 + 0.5;
                return uv;
            }
            
            float3 posterize(float3 color, float steps)
            {
                float3 posterized = floor(color * steps) / steps;
                float3 next = ceil(color * steps) / steps;
                float3 blend = frac(color * steps);
                return lerp(posterized, next, blend * blend);
            }
            
            // Optimized phosphor glow - reduced samples
            float3 phosphorGlow(float3 color, float2 uv)
            {
                float3 glow = color;
                const int samples = 3; // Reduced from 4
                float invSamples = 1.0 / float(samples);
                
                [unroll]
                for(int i = 1; i <= samples; i++)
                {
                    float2 offset = float2(0, i) * (1.0 / _ScreenParams.y) * 2.0;
                    float3 sampleUp = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + offset).rgb;
                    float3 sampleDown = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - offset).rgb;
                    float weight = exp(-float(i * i) * 0.5);
                    glow += (sampleUp + sampleDown) * weight * 0.25;
                }
                
                return glow / (1.0 + 0.5 * samples);
            }
            
            float3 applyWarmth(float3 color, float warmth)
            {
                color.r += warmth * 0.1;
                color.g += warmth * 0.05;
                color.b -= warmth * 0.15;
                return color;
            }
            
            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                
                // ===== CRT CURVATURE =====
                float2 curvedUV = CRTCurve(uv);
                
                // Optimized edge fade using step()
                float2 edge = smoothstep(0.0, 0.1, curvedUV) * smoothstep(1.0, 0.9, curvedUV);
                float edgeFade = edge.x * edge.y;
                
                // Early exit with step() instead of if
                float edgeMask = step(EDGE_FADE_THRESHOLD, edgeFade);
                float3 edgeColor = float3(0, 0, 0);
                
                // ===== PIXELATION =====
                float2 pixelUV = floor(curvedUV * _ScreenParams.xy / _PixelSize) * _PixelSize / _ScreenParams.xy;
                float2 sampleUV = pixelUV + (_PixelSize / _ScreenParams.xy) * 0.5;
                
                // ===== CHROMATIC ABERRATION (optimized - single sample for G, offset for R/B) =====
                float aberration = _ChromaticAberration;
                float3 baseColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, sampleUV).rgb;
                float r = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, sampleUV + float2(aberration, 0)).r;
                float b = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, sampleUV - float2(aberration, 0)).b;
                float3 color = float3(r, baseColor.g, b);
                
                // ===== PHOSPHOR GLOW =====
                float phosphorMask = step(0.01, _PhosphorGlow);
                color = lerp(color, phosphorGlow(color, sampleUV), _PhosphorGlow * phosphorMask);
                
                // ===== COLOR DEPTH REDUCTION =====
                color = posterize(color, _ColorDepth);
                
                // ===== SCANLINES (improved variation) =====
                float scanlinePhase = curvedUV.y * _ScreenParams.y * 2.0 - _Time.y * _ScanlineSpeed;
                float scanline = sin(scanlinePhase) * 0.5 + 0.5;
                float scanlineThickness = abs(sin(_Time.y * _ScanlineSpeed * 0.5)) * 0.2 + 0.1;
                scanline = pow(scanline, 2.0);
                color *= lerp(1.0, scanline * 0.4 + 0.6, _ScanlineIntensity);
                
                // ===== SCREEN DOOR EFFECT =====
                float screenDoorMask = step(0.01, _ScreenDoorEffect);
                float2 subpixel = frac(curvedUV * _ScreenParams.xy / _PixelSize);
                float3 mask = float3(
                    step(SUBPIXEL_RED_THRESHOLD, subpixel.x),
                    step(SUBPIXEL_RED_THRESHOLD, subpixel.x) * step(subpixel.x, SUBPIXEL_BLUE_THRESHOLD),
                    step(SUBPIXEL_BLUE_THRESHOLD, subpixel.x)
                );
                mask = lerp(float3(1, 1, 1), mask * 0.85 + 0.15, _ScreenDoorEffect * screenDoorMask);
                color *= mask;
                
                // ===== VIGNETTE =====
                float2 vignetteUV = curvedUV * (1.0 - curvedUV);
                float vignette = vignetteUV.x * vignetteUV.y * VIGNETTE_MULTIPLIER;
                vignette = pow(saturate(vignette), 0.4);
                vignette = lerp(1.0, vignette, _VignetteStrength * 0.4);
                color *= vignette;
                
                // ===== COLOR TEMPERATURE =====
                color = applyWarmth(color, _WarmthShift);
                
                // ===== BRIGHTNESS & CONTRAST =====
                color = (color - 0.5) * _Contrast + 0.5;
                color *= _Brightness;
                color = saturate(color);
                
                // ===== SUBTLE NOISE =====
                float noise = hash(curvedUV + frac(_Time.y * 0.3) * 0.1);
                color += (noise - 0.5) * _NoiseAmount * 0.5;
                
                // ===== COLOR BLEEDING (optimized - single sample) =====
                float2 bleedOffset = float2(2.0, 0.0) / _ScreenParams.xy;
                float3 bleedSample = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, sampleUV + bleedOffset).rgb;
                color.r = lerp(color.r, (color.r + bleedSample.r) * 0.5, _ColorBleeding * 0.6);
                color.b = lerp(color.b, (color.b + SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, sampleUV - bleedOffset).b) * 0.5, _ColorBleeding * 0.6);
                
                // ===== GLASS REFLECTION (simplified/removed if not needed) =====
                // Removed for performance - can be re-added if needed
                
                // ===== FINAL EDGE BLEND =====
                color = lerp(edgeColor, color, edgeFade * edgeMask);
                
                return float4(saturate(color), 1.0);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "CopyExcluded"
            
            ZTest Always
            ZWrite Off
            Cull Off
            
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
            
            half4 Frag(Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);
            }
            ENDHLSL
        }
    }
}