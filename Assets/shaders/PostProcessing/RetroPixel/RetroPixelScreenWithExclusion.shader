Shader "Custom/RetroPixelScreenWithExclusion"
{
    Properties
    {
        [Header(Core Settings)]
        _PixelSize ("Pixel Size", Range(1, 8)) = 2
        _ColorDepth ("Color Depth", Range(4, 32)) = 24
        _Brightness ("Brightness", Range(0.5, 1.5)) = 1.0
        _Contrast ("Contrast", Range(0.5, 2.0)) = 1.1

        [Header(CRT Geometry)]
        _CRTCurvature ("CRT Curvature", Range(0, 0.3)) = 0.08
        _CornerRadius ("Corner Radius", Range(0, 0.15)) = 0.03
        _CornerSharpness ("Corner Sharpness", Range(2, 20)) = 8

        [Header(Scanlines)]
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.15
        _ScanlineCount ("Scanline Density", Range(0.5, 2.0)) = 1.0
        _ScanlineBloom ("Scanline Bloom", Range(0, 1)) = 0.3
        _ScanlineSpeed ("Scanline Roll Speed", Float) = 0.0

        [Header(Phosphor Display)]
        _PhosphorType ("Phosphor Type (0=None, 1=Aperture, 2=Shadow)", Range(0, 2)) = 1
        _PhosphorIntensity ("Phosphor Intensity", Range(0, 1)) = 0.25
        _PhosphorGlow ("Phosphor Glow", Range(0, 1)) = 0.4
        _PhosphorBloom ("Phosphor Bloom Radius", Range(0, 4)) = 1.5

        [Header(Signal Interference)]
        _ChromaticAberration ("Chromatic Aberration", Range(0, 0.01)) = 0.003
        _ColorBleeding ("Color Bleeding", Range(0, 0.2)) = 0.05
        _SignalNoise ("Signal Noise", Range(0, 0.1)) = 0.02
        _HorizontalJitter ("Horizontal Jitter", Range(0, 0.005)) = 0.001
        _RollingBandIntensity ("Rolling Band Intensity", Range(0, 0.3)) = 0.05
        _RollingBandSpeed ("Rolling Band Speed", Float) = 2.0

        [Header(Color Grading)]
        _WarmthShift ("Warmth Shift", Range(-1, 1)) = 0.15
        _Saturation ("Saturation", Range(0, 2)) = 1.05
        _GammaCorrection ("CRT Gamma", Range(1.0, 3.0)) = 2.2
        _ShadowTint ("Shadow Tint (RGB)", Color) = (0.02, 0.0, 0.05, 1)
        _HighlightTint ("Highlight Tint (RGB)", Color) = (1.0, 0.98, 0.9, 1)

        [Header(Vignette and Reflection)]
        _VignetteStrength ("Vignette Strength", Range(0, 1)) = 0.25
        _VignetteColor ("Vignette Color", Color) = (0.0, 0.0, 0.02, 1)
        _GlassReflection ("Glass Reflection", Range(0, 0.5)) = 0.08
        _ReflectionAngle ("Reflection Angle", Range(0, 6.28)) = 0.8

        [Header(Temporal Effects)]
        _FlickerIntensity ("Flicker Intensity", Range(0, 0.1)) = 0.015
        _FlickerSpeed ("Flicker Speed", Float) = 12.0
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

            // ============================================
            // PARAMETERS
            // ============================================

            float _PixelSize;
            float _ColorDepth;
            float _Brightness;
            float _Contrast;

            float _CRTCurvature;
            float _CornerRadius;
            float _CornerSharpness;

            float _ScanlineIntensity;
            float _ScanlineCount;
            float _ScanlineBloom;
            float _ScanlineSpeed;

            float _PhosphorType;
            float _PhosphorIntensity;
            float _PhosphorGlow;
            float _PhosphorBloom;

            float _ChromaticAberration;
            float _ColorBleeding;
            float _SignalNoise;
            float _HorizontalJitter;
            float _RollingBandIntensity;
            float _RollingBandSpeed;

            float _WarmthShift;
            float _Saturation;
            float _GammaCorrection;
            float4 _ShadowTint;
            float4 _HighlightTint;

            float _VignetteStrength;
            float4 _VignetteColor;
            float _GlassReflection;
            float _ReflectionAngle;

            float _FlickerIntensity;
            float _FlickerSpeed;

            // ============================================
            // NOISE FUNCTIONS
            // ============================================

            float hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float hash12(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float2 hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            // Value noise for smoother grain
            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // Smoothstep

                float a = hash12(i);
                float b = hash12(i + float2(1, 0));
                float c = hash12(i + float2(0, 1));
                float d = hash12(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Film grain with natural look
            float filmGrain(float2 uv, float time)
            {
                float2 noiseUV = uv * 200.0;
                float grain = valueNoise(noiseUV + time * 100.0);
                grain = grain * 2.0 - 1.0;

                // Add some variation at different scales
                grain += (valueNoise(noiseUV * 0.5 + time * 50.0) * 2.0 - 1.0) * 0.5;

                return grain * 0.5;
            }

            // ============================================
            // CRT GEOMETRY
            // ============================================

            float2 CRTCurve(float2 uv)
            {
                uv = uv * 2.0 - 1.0;
                float2 offset = abs(uv.yx) * _CRTCurvature;
                uv = uv + uv * offset * offset;
                uv = uv * 0.5 + 0.5;
                return uv;
            }

            // Rounded corner mask
            float roundedCornerMask(float2 uv)
            {
                float2 centered = abs(uv - 0.5) * 2.0;
                float2 cornerDist = centered - (1.0 - _CornerRadius);
                cornerDist = max(cornerDist, 0.0);
                float dist = length(cornerDist) / _CornerRadius;
                return 1.0 - pow(saturate(dist), _CornerSharpness);
            }

            // ============================================
            // PHOSPHOR PATTERNS
            // ============================================

            // Aperture grille (Sony Trinitron style - vertical stripes)
            float3 apertureGrille(float2 uv, float3 color)
            {
                float pixelX = uv.x * _ScreenParams.x / _PixelSize;
                float subpixel = frac(pixelX);

                float3 mask;
                // RGB stripe pattern with smooth transitions
                mask.r = smoothstep(0.0, 0.15, subpixel) * smoothstep(0.5, 0.35, subpixel);
                mask.g = smoothstep(0.25, 0.4, subpixel) * smoothstep(0.75, 0.6, subpixel);
                mask.b = smoothstep(0.55, 0.7, subpixel) * smoothstep(1.0, 0.85, subpixel);

                // Normalize and add base brightness
                mask = mask * 0.7 + 0.3;

                return color * lerp(float3(1, 1, 1), mask, _PhosphorIntensity);
            }

            // Shadow mask (dot triad pattern)
            float3 shadowMask(float2 uv, float3 color)
            {
                float2 pixelPos = uv * _ScreenParams.xy / _PixelSize;
                float2 cell = frac(pixelPos);
                float row = frac(floor(pixelPos.y) * 0.5);

                // Offset every other row
                cell.x = frac(cell.x + row * 0.5);

                float3 mask;
                float2 center1 = float2(0.25, 0.5);
                float2 center2 = float2(0.5, 0.5);
                float2 center3 = float2(0.75, 0.5);

                float dotSize = 0.3;
                mask.r = 1.0 - smoothstep(0.0, dotSize, length(cell - center1));
                mask.g = 1.0 - smoothstep(0.0, dotSize, length(cell - center2));
                mask.b = 1.0 - smoothstep(0.0, dotSize, length(cell - center3));

                mask = mask * 0.8 + 0.2;

                return color * lerp(float3(1, 1, 1), mask, _PhosphorIntensity);
            }

            // ============================================
            // SCANLINES
            // ============================================

            float scanlineEffect(float2 uv, float brightness)
            {
                float scanY = uv.y * _ScreenParams.y * _ScanlineCount / _PixelSize;
                float scan = sin(scanY * 3.14159 + _Time.y * _ScanlineSpeed);

                // Shape the scanline - darker between lines
                scan = scan * 0.5 + 0.5;
                scan = pow(scan, 1.5);

                // Bloom effect - brighter pixels have softer scanlines
                float bloom = lerp(1.0, brightness, _ScanlineBloom);
                scan = lerp(scan, 1.0, bloom * 0.5);

                return lerp(1.0, scan * 0.6 + 0.4, _ScanlineIntensity);
            }

            // ============================================
            // SIGNAL EFFECTS
            // ============================================

            float2 signalDistortion(float2 uv, float time)
            {
                // Horizontal jitter (like bad VHS tracking)
                float jitterLine = floor(uv.y * _ScreenParams.y);
                float jitter = (hash11(jitterLine + time * 100.0) * 2.0 - 1.0) * _HorizontalJitter;

                // Rolling bands
                float bandPhase = uv.y * 4.0 - time * _RollingBandSpeed;
                float band = sin(bandPhase) * sin(bandPhase * 3.7) * 0.5 + 0.5;
                jitter += band * _HorizontalJitter * 0.5;

                return float2(uv.x + jitter, uv.y);
            }

            float rollingBand(float2 uv, float time)
            {
                float bandY = frac(uv.y * 0.3 - time * _RollingBandSpeed * 0.1);
                float band = smoothstep(0.0, 0.1, bandY) * smoothstep(0.3, 0.2, bandY);
                return 1.0 - band * _RollingBandIntensity;
            }

            // ============================================
            // COLOR PROCESSING
            // ============================================

            float3 posterize(float3 color, float steps)
            {
                float3 posterized = floor(color * steps) / steps;
                float3 next = ceil(color * steps) / steps;
                float3 blend = frac(color * steps);
                // Smooth transition with slight S-curve
                blend = blend * blend * (3.0 - 2.0 * blend);
                return lerp(posterized, next, blend);
            }

            float3 applyGamma(float3 color, float gamma)
            {
                return pow(max(color, 0.0001), 1.0 / gamma);
            }

            float3 applySaturation(float3 color, float saturation)
            {
                float luminance = dot(color, float3(0.299, 0.587, 0.114));
                return lerp(float3(luminance, luminance, luminance), color, saturation);
            }

            float3 colorGrade(float3 color)
            {
                float luminance = dot(color, float3(0.299, 0.587, 0.114));

                // Apply shadow and highlight tinting
                float3 shadows = lerp(color, color * _ShadowTint.rgb * 2.0, (1.0 - luminance) * 0.3);
                float3 highlights = lerp(shadows, shadows * _HighlightTint.rgb, luminance * 0.5);

                // Warmth shift
                float3 warm = highlights;
                warm.r += _WarmthShift * 0.08;
                warm.g += _WarmthShift * 0.02;
                warm.b -= _WarmthShift * 0.1;

                return warm;
            }

            // ============================================
            // BLOOM / GLOW
            // ============================================

            float3 phosphorBloom(float2 uv, float3 centerColor)
            {
                float3 bloom = centerColor;
                float totalWeight = 1.0;

                // Sample in a cross pattern for glow
                const int samples = 4;
                float2 texelSize = 1.0 / _ScreenParams.xy * _PhosphorBloom;

                [unroll]
                for (int i = 1; i <= samples; i++)
                {
                    float weight = exp(-float(i) * 0.5);

                    // Horizontal samples
                    bloom += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(texelSize.x * i, 0)).rgb * weight;
                    bloom += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - float2(texelSize.x * i, 0)).rgb * weight;

                    // Vertical samples
                    bloom += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(0, texelSize.y * i)).rgb * weight;
                    bloom += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - float2(0, texelSize.y * i)).rgb * weight;

                    totalWeight += weight * 4.0;
                }

                return bloom / totalWeight;
            }

            // ============================================
            // VIGNETTE & REFLECTIONS
            // ============================================

            float3 applyVignette(float3 color, float2 uv)
            {
                float2 vignetteUV = uv * (1.0 - uv);
                float vignette = vignetteUV.x * vignetteUV.y * 15.0;
                vignette = pow(saturate(vignette), 0.35);

                // Blend to vignette color instead of pure black
                float3 vignetteColor = _VignetteColor.rgb;
                return lerp(vignetteColor, color, lerp(1.0, vignette, _VignetteStrength));
            }

            float3 glassReflection(float3 color, float2 uv)
            {
                // Simulate glass reflection highlight
                float2 reflectUV = uv - 0.5;
                float angle = _ReflectionAngle;
                float2 reflectDir = float2(cos(angle), sin(angle));

                float reflection = dot(reflectUV, reflectDir);
                reflection = 1.0 - abs(reflection) * 3.0;
                reflection = pow(saturate(reflection), 3.0);

                // Add subtle edge highlight
                float2 edgeDist = abs(uv - 0.5) * 2.0;
                float edgeHighlight = max(edgeDist.x, edgeDist.y);
                edgeHighlight = pow(edgeHighlight, 4.0) * 0.3;

                return color + (reflection + edgeHighlight) * _GlassReflection * 0.5;
            }

            // ============================================
            // MAIN FRAGMENT SHADER
            // ============================================

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float time = _Time.y;

                // ===== CRT CURVATURE =====
                float2 curvedUV = CRTCurve(uv);

                // ===== BOUNDS CHECK =====
                float2 outsideBounds = step(curvedUV, float2(0, 0)) + step(float2(1, 1), curvedUV);
                if (outsideBounds.x + outsideBounds.y > 0)
                    return float4(0, 0, 0, 1);

                // ===== CORNER MASK =====
                float cornerMask = roundedCornerMask(curvedUV);

                // ===== SIGNAL DISTORTION =====
                float2 distortedUV = signalDistortion(curvedUV, time);

                // ===== PIXELATION =====
                float2 pixelUV = floor(distortedUV * _ScreenParams.xy / _PixelSize) * _PixelSize / _ScreenParams.xy;
                float2 sampleUV = pixelUV + (_PixelSize / _ScreenParams.xy) * 0.5;

                // ===== CHROMATIC ABERRATION =====
                float2 aberrationOffset = (curvedUV - 0.5) * _ChromaticAberration;
                float3 color;
                color.r = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, sampleUV + aberrationOffset).r;
                color.g = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, sampleUV).g;
                color.b = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, sampleUV - aberrationOffset).b;

                // ===== COLOR BLEEDING =====
                float2 bleedOffset = float2(_ColorBleeding * 3.0 / _ScreenParams.x, 0);
                float3 bleedSample = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, sampleUV + bleedOffset).rgb;
                color = lerp(color, (color + bleedSample) * 0.5, _ColorBleeding * 0.5);

                // ===== PHOSPHOR BLOOM =====
                float3 bloomColor = phosphorBloom(sampleUV, color);
                color = lerp(color, bloomColor, _PhosphorGlow * 0.5);

                // ===== COLOR DEPTH =====
                color = posterize(color, _ColorDepth);

                // ===== PHOSPHOR PATTERN =====
                if (_PhosphorType > 0.5 && _PhosphorType < 1.5)
                    color = apertureGrille(curvedUV, color);
                else if (_PhosphorType >= 1.5)
                    color = shadowMask(curvedUV, color);

                // ===== SCANLINES =====
                float brightness = dot(color, float3(0.299, 0.587, 0.114));
                color *= scanlineEffect(curvedUV, brightness);

                // ===== ROLLING INTERFERENCE BAND =====
                color *= rollingBand(curvedUV, time);

                // ===== FLICKER =====
                float flicker = 1.0 + (hash11(floor(time * _FlickerSpeed)) * 2.0 - 1.0) * _FlickerIntensity;
                color *= flicker;

                // ===== COLOR GRADING =====
                color = colorGrade(color);
                color = applySaturation(color, _Saturation);

                // ===== BRIGHTNESS & CONTRAST =====
                color = (color - 0.5) * _Contrast + 0.5;
                color *= _Brightness;

                // ===== CRT GAMMA =====
                color = applyGamma(color, _GammaCorrection);

                // ===== SIGNAL NOISE =====
                float noise = filmGrain(curvedUV, time);
                color += noise * _SignalNoise;

                // ===== VIGNETTE =====
                color = applyVignette(color, curvedUV);

                // ===== GLASS REFLECTION =====
                color = glassReflection(color, curvedUV);

                // ===== APPLY CORNER MASK =====
                color *= cornerMask;

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
