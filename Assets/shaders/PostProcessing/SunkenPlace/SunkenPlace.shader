Shader "Custom/SunkenPlace"
{
    Properties
    {
        _SinkAmount ("Sink Amount", Range(0, 1)) = 0.5
        _FallSpeed ("Fall Speed", Float) = 0.5
        _VignetteIntensity ("Vignette Intensity", Range(0, 1)) = 0.9
        _ChromaticAberration ("Chromatic Aberration", Range(0, 0.05)) = 0.02
        _ScreenGlow ("Screen Glow", Range(0, 2)) = 0.5
        _Darkness ("Darkness", Range(0, 1)) = 0.95
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
            
            float _SinkAmount;
            float _FallSpeed;
            float _VignetteIntensity;
            float _ChromaticAberration;
            float _ScreenGlow;
            float _Darkness;
            
            // Simple noise for subtle movement
            float hash(float n)
            {
                return frac(sin(n) * 43758.5453);
            }
            
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i.x + i.y * 57.0);
                float b = hash(i.x + 1.0 + i.y * 57.0);
                float c = hash(i.x + (i.y + 1.0) * 57.0);
                float d = hash(i.x + 1.0 + (i.y + 1.0) * 57.0);
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float2 center = 0.5;
                
                float time = _Time.y * _FallSpeed;
                
                // ===== SHRINKING SCREEN EFFECT =====
                float screenSize = lerp(1.0, 0.15, _SinkAmount);
                
                // Add subtle drift/float to the screen position
                float2 drift = float2(
                    sin(time * 0.3) * 0.02,
                    cos(time * 0.4) * 0.015
                ) * _SinkAmount;
                
                // Calculate screen UV without distortion
                float2 screenUV = (uv - center - drift) / screenSize + center;
                
                // Check bounds - proper rectangular screen (no aspect distortion)
                float2 screenBounds = abs(uv - center - drift) / screenSize;
                bool insideScreen = screenBounds.x < 0.5 && screenBounds.y < 0.5;
                
                // ===== CHROMATIC ABERRATION =====
                float aberration = _ChromaticAberration * _SinkAmount;
                float r = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, screenUV + float2(aberration, 0)).r;
                float g = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, screenUV).g;
                float b = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, screenUV - float2(aberration, 0)).b;
                float3 sceneColor = float3(r, g, b);
                
                // Add CRT-like scanlines
                float scanline = sin(screenUV.y * _ScreenParams.y * 2.0 - time * 10.0) * 0.05 + 0.95;
                sceneColor *= scanline;
                
                // Darken the screen as it gets further away
                float screenDarkening = lerp(1.0, 0.7, _SinkAmount);
                sceneColor *= screenDarkening;
                
                // ===== SCREEN GLOW =====
                float2 toScreen = uv - center - drift;
                float screenDist = length(toScreen / screenSize);
                
                float glow = exp(-screenDist * 3.0) * _ScreenGlow * _SinkAmount;
                float3 glowColor = sceneColor * glow * 0.5;
                
                // ===== THE VOID =====
                float3 voidColor = float3(0.0, 0.0, 0.0);
                
                float voidNoise = noise(uv * 100.0 + time * 0.1) * 0.01;
                voidColor += voidNoise * (1.0 - _Darkness);
                
                voidColor += float3(0.0, 0.0, 0.01) * (1.0 - _Darkness) * _SinkAmount;
                
                // ===== COMBINE =====
                float3 finalColor;
                
                if (insideScreen)
                {
                    finalColor = sceneColor;
                    
                    // Edge fade for the screen (smooth borders)
                    float edgeFade = smoothstep(0.5, 0.47, max(screenBounds.x, screenBounds.y));
                    finalColor *= edgeFade;
                }
                else
                {
                    finalColor = voidColor;
                    finalColor += glowColor;
                }
                
                // ===== VIGNETTE - using centered distance =====
                float2 centeredPos = uv - center;
                float dist = length(centeredPos);
                float vignette = 1.0 - dist * _VignetteIntensity * _SinkAmount;
                vignette = smoothstep(0.0, 1.0, vignette);
                finalColor *= vignette;
                
                float globalDarkness = lerp(1.0, (1.0 - _Darkness), _SinkAmount);
                finalColor *= globalDarkness;
                
                // Film grain
                float grain = hash(dot(uv, float2(12.9898, 78.233)) + time * 100.0);
                finalColor += (grain - 0.5) * 0.015;
                
                // Desaturate as you sink
                float luminance = dot(finalColor, float3(0.299, 0.587, 0.114));
                finalColor = lerp(finalColor, float3(luminance, luminance, luminance), _SinkAmount * 0.6);
                
                return float4(saturate(finalColor), 1.0);
            }
            ENDHLSL
        }
    }
}