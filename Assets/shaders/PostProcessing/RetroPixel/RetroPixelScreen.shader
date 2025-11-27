Shader "Custom/RetroPixelScreen"
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
        ZWrite Off Cull Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            
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
            
            // Better noise function
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            // Improved CRT barrel distortion
            float2 CRTCurve(float2 uv)
            {
                uv = uv * 2.0 - 1.0;
                float2 offset = abs(uv.yx) * _CRTCurvature;
                uv = uv + uv * offset * offset;
                uv = uv * 0.5 + 0.5;
                return uv;
            }
            
            // Better posterize with smoothing
            float3 posterize(float3 color, float steps)
            {
                float3 posterized = floor(color * steps) / steps;
                float3 next = ceil(color * steps) / steps;
                float3 blend = frac(color * steps);
                return lerp(posterized, next, blend * blend); // Smooth blend
            }
            
            // Enhanced phosphor glow with better falloff
            float3 phosphorGlow(float3 color, float2 uv)
            {
                float3 glow = color;
                const int samples = 4;
                
                [unroll]
                for(int i = -samples; i <= samples; i++)
                {
                    float2 offset = float2(0, i) * (1.0 / _ScreenParams.y) * 3.0;
                    float3 sample = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + offset).rgb;
                    float distance = abs(float(i)) / float(samples);
                    float weight = exp(-distance * distance * 2.0); // Gaussian falloff
                    glow += sample * weight * 0.3;
                }
                
                return glow / (1.0 + 0.3 * (samples * 2 + 1));
            }
            
            // Warm color temperature shift
            float3 applyWarmth(float3 color, float warmth)
            {
                // Increase red/yellow, decrease blue
                color.r += warmth * 0.1;
                color.g += warmth * 0.05;
                color.b -= warmth * 0.15;
                return color;
            }
            
            // Enhanced glass reflection effect
            float3 glassReflection(float2 uv, float strength)
            {
                float2 reflectUV = uv;
                reflectUV.y = 1.0 - reflectUV.y; // Mirror vertically
                
                float3 reflect = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, reflectUV * 0.1 + float2(0, 0.9)).rgb;
                return reflect * strength * 0.5;
            }
            
            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                
                // ===== CRT CURVATURE =====
                float2 curvedUV = CRTCurve(uv);
                
                // Edge fade for CRT effect (soft edges)
                float2 edge = smoothstep(0.0, 0.1, curvedUV) * smoothstep(1.0, 0.9, curvedUV);
                float edgeFade = edge.x * edge.y;
                
                if (edgeFade < 0.01)
                    return float4(0.02, 0.01, 0.05, 1); // Dark blue bezel
                
                // ===== PIXELATION =====
                float2 pixelUV = floor(curvedUV * _ScreenParams.xy / _PixelSize) * _PixelSize / _ScreenParams.xy;
                float2 sampleUV = pixelUV + (_PixelSize / _ScreenParams.xy) * 0.5;
                
                // ===== CHROMATIC ABERRATION =====
                float aberration = _ChromaticAberration;
                float r = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, sampleUV + float2(aberration, 0)).r;
                float g = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, sampleUV).g;
                float b = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, sampleUV - float2(aberration, 0)).b;
                float3 color = float3(r, g, b);
                
                // ===== PHOSPHOR GLOW =====
                if (_PhosphorGlow > 0.01)
                {
                    color = phosphorGlow(color, sampleUV);
                }
                
                // ===== COLOR DEPTH REDUCTION =====
                color = posterize(color, _ColorDepth);
                
                // ===== SCANLINES (with better visuals) =====
                float scanline = sin(curvedUV.y * _ScreenParams.y * 2.0 - _Time.y * _ScanlineSpeed) * 0.5 + 0.5;
                float scanlineHeight = abs(sin(_Time.y * _ScanlineSpeed * 0.5)) * 0.2 + 0.1;
                scanline = pow(scanline, 2.0); // Softer scanlines
                color *= mix(1.0, scanline * 0.4 + 0.6, _ScanlineIntensity);
                
                // ===== SCREEN DOOR EFFECT (RGB subpixels) =====
                if (_ScreenDoorEffect > 0.01)
                {
                    float2 subpixel = frac(curvedUV * _ScreenParams.xy / _PixelSize);
                    float3 mask = float3(
                        step(0.33, subpixel.x),
                        step(0.33, subpixel.x) * step(subpixel.x, 0.66),
                        step(0.66, subpixel.x)
                    );
                    mask = mix(float3(1, 1, 1), mask * 0.85 + 0.15, _ScreenDoorEffect);
                    color *= mask;
                }
                
                // ===== VIGNETTE (smoother) =====
                float2 vignetteUV = curvedUV * (1.0 - curvedUV);
                float vignette = vignetteUV.x * vignetteUV.y * 15.0;
                vignette = pow(saturate(vignette), 0.4);
                vignette = mix(1.0, vignette, _VignetteStrength * 0.4);
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
                
                // ===== COLOR BLEEDING =====
                float2 bleedOffset = float2(2.0, 0.0) / _ScreenParams.xy;
                float3 bleedRight = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, sampleUV + bleedOffset).rgb;
                float3 bleedLeft = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, sampleUV - bleedOffset).rgb;
                
                color.r = mix(color.r, (color.r + bleedRight.r) * 0.5, _ColorBleeding * 0.6);
                color.b = mix(color.b, (color.b + bleedLeft.b) * 0.5, _ColorBleeding * 0.6);
                
                // ===== GLASS REFLECTION =====
                if (_GlassReflection > 0.01)
                {
                    float3 reflection = glassReflection(curvedUV, _GlassReflection);
                    color = mix(color, reflection, _GlassReflection * edgeFade * 0.3);
                }
                
                // ===== FINAL EDGE BLEND =====
                color *= edgeFade;
                
                return float4(saturate(color), 1.0);
            }
            ENDHLSL
        }
    }
}