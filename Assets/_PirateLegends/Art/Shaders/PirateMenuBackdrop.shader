Shader "Pirate Legends/Menu/Animated Backdrop"
{
    Properties
    {
        _BaseMap ("Moonlit cove", 2D) = "black" {}
        _PortMap ("Lantern harbor", 2D) = "black" {}
        _DayMap ("Tropical lagoon", 2D) = "black" {}
        _StormMap ("Stormy sea", 2D) = "black" {}
        _BackdropIndex ("Backdrop index", Float) = 0
        _ReduceMotion ("Reduce motion", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_PortMap);
            SAMPLER(sampler_PortMap);
            TEXTURE2D(_DayMap);
            SAMPLER(sampler_DayMap);
            TEXTURE2D(_StormMap);
            SAMPLER(sampler_StormMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _BackdropIndex;
                float _ReduceMotion;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            float2 AnimateWater(float2 uv, float horizon, float time)
            {
                float water = 1.0 - smoothstep(horizon - 0.12, horizon, uv.y);
                float wave = sin(uv.y * 73.0 + uv.x * 16.0 - time * 2.8);
                float ripple = sin(uv.y * 140.0 - uv.x * 21.0 + time * 2.1);
                uv.x += water * (wave * 0.010 + ripple * 0.003);
                uv.y += water * (wave * 0.0025 + ripple * 0.0015);
                return saturate(uv);
            }

            float WaveGlints(float2 uv, float horizon, float time)
            {
                float depth = horizon - uv.y;
                float water = smoothstep(0.0, 0.04, depth) * (1.0 - smoothstep(0.24, 0.39, depth));
                float travel = sqrt(max(depth, 0.0)) * 61.0 - time * 2.8;
                float bend = sin(uv.x * 24.0 + travel * 0.33) * 0.8;
                float crest = smoothstep(0.73, 0.98, sin(travel + bend));
                float patches = smoothstep(0.04, 0.74, sin(uv.x * 39.0 + travel * 0.83)
                    * sin(uv.x * 67.0 - travel * 0.52));
                return water * crest * patches;
            }

            float Gull(float2 uv, float2 center, float size, float phase)
            {
                float2 localUV = (uv - center) / size;
                localUV.y *= 0.56;
                float x = abs(localUV.x);
                if (x > 1.1 || abs(localUV.y) > 0.7)
                    return 0.0;

                float flap = sin(phase);
                float wingCurve = (flap * 0.43 + 0.18) * x + 0.16 * sin(x * 3.14) - 0.12 * x * x;
                float width = lerp(0.115, 0.025, x);
                float wing = (1.0 - smoothstep(width * 0.55, width, abs(localUV.y - wingCurve)))
                    * (1.0 - smoothstep(0.87, 1.0, x));
                float body = 1.0 - smoothstep(0.78, 1.0, length(localUV / float2(0.15, 0.16)));
                return max(wing, body);
            }

            float Flock(float2 uv, float time)
            {
                float crossing = frac((time + 3.0) / 14.0);
                float x = lerp(1.19, 0.02, crossing);
                float y = 0.76 + sin(crossing * 4.8) * 0.055;
                float flap = time * 8.7;
                float birds = Gull(uv, float2(x, y), 0.014, flap);
                birds = max(birds, Gull(uv, float2(x + 0.055, y + 0.038), 0.011, flap + 1.6));
                birds = max(birds, Gull(uv, float2(x + 0.103, y + 0.002), 0.009, flap + 3.2));
                return birds;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float time = _Time.y * (1.0 - _ReduceMotion);
                float2 uv = input.uv;
                float horizon = 0.42;
                half3 glintColor = half3(0.013, 0.074, 0.090);
                half3 color;
                if (_BackdropIndex < 0.5)
                    color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, AnimateWater(uv, horizon, time)).rgb;
                else if (_BackdropIndex < 1.5)
                {
                    uv.y -= 0.055;
                    horizon = 0.33;
                    glintColor = lerp(half3(0.010, 0.045, 0.068), half3(0.135, 0.068, 0.020),
                        smoothstep(0.60, 0.95, uv.x));
                    color = SAMPLE_TEXTURE2D(_PortMap, sampler_PortMap, AnimateWater(uv, horizon, time)).rgb;
                }
                else if (_BackdropIndex < 2.5)
                {
                    glintColor = half3(0.12, 0.22, 0.22);
                    uv.x += smoothstep(0.65, 0.95, uv.y) * sin(time * 0.13) * 0.012;
                    color = SAMPLE_TEXTURE2D(_DayMap, sampler_DayMap, AnimateWater(uv, horizon, time)).rgb;
                }
                else
                {
                    glintColor = half3(0.052, 0.079, 0.11);
                    color = SAMPLE_TEXTURE2D(_StormMap, sampler_StormMap, AnimateWater(uv, horizon, time * 1.5)).rgb;
                    float2 rainUV = float2((uv.x + uv.y * 0.20) * 108.0, uv.y * 11.0 + time * 5.5);
                    rainUV.y += sin(floor(rainUV.x) * 4.41) * 0.6;
                    float rain = (1.0 - smoothstep(0.025, 0.085, abs(frac(rainUV.x) - 0.5)))
                        * smoothstep(0.76, 0.96, frac(rainUV.y));
                    color += rain * (1.0 - _ReduceMotion) * half3(0.07, 0.095, 0.13);
                }
                color += WaveGlints(uv, horizon, time) * glintColor;

                float fogBand = smoothstep(0.28, 0.42, uv.y) * (1.0 - smoothstep(0.42, 0.53, uv.y));
                float fog = 0.026 * fogBand * smoothstep(-0.25, 0.84,
                    sin(uv.x * 14.0 + uv.y * 21.0 - time * 0.48) + 0.35 * sin(uv.x * 33.0 - time * 0.82));
                color += fog * half3(0.26, 0.53, 0.63);
                color = lerp(color, half3(0.003, 0.009, 0.014), Flock(uv, time) * 0.84 * (1.0 - _ReduceMotion));
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
