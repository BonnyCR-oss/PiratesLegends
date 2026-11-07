Shader "PirateLegends/FireAbility"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float2 p = i.uv * 2 - 1;
                p.x += sin(p.y * 8 - _Time.y * 18) * .12 * (p.y + 1);
                p.x *= 1.2 + max(p.y, 0) * 1.5;
                float falloff = saturate(1 - dot(p, p));
                float flicker = .8 + .2 * sin(p.y * 19 - _Time.y * 25 + sin(p.x * 13));
                half3 hot = lerp(i.color.rgb, half3(1, .95, .55), pow(falloff, 5) * .65);
                return half4(hot * 1.8, falloff * falloff * flicker * i.color.a);
            }
            ENDHLSL
        }
    }
}
