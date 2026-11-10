Shader "PirateLegends/PrototypeOcean"
{
 Properties
 {
     _DeepColor("Deep water", Color) = (0.018,0.19,0.29,1)
     _ShallowColor("Shallow water", Color) = (0.08,0.68,0.63,1)
     _FoamColor("Sea foam", Color) = (0.84,0.97,0.91,1)
     _WaveHeight("Wave height", Range(0,1)) = 0.14
     _IslandHeight("Island elevation", 2D) = "black" {}
 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Tags { "LightMode"="UniversalForward" }
   ZWrite Off
   Cull Back
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

   TEXTURE2D(_IslandHeight);
   SAMPLER(sampler_IslandHeight);
   CBUFFER_START(UnityPerMaterial)
       half4 _DeepColor, _ShallowColor, _FoamColor;
       float _WaveHeight;
   CBUFFER_END
   float4 _SeaState;
   float4x4 _HullWorldToLocal;
   float4 _HullRange;
   float4 _HullWidths[17];
   static const float2 Swell = float2(.109, .057);
   static const float2 Crossing = float2(-.097, .176);
   static const float2 Chop = float2(.264, -.157);

   struct Attributes { float4 positionOS : POSITION; };
   struct Varyings
   {
       float4 positionCS : SV_POSITION;
       float3 world : TEXCOORD0;
       half fog : TEXCOORD1;
   };

   float Hash(float2 p)
   {
       return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
   }

   float Noise(float2 p)
   {
       float2 cell = floor(p);
       float2 f = frac(p);
       f = f * f * (3 - 2 * f);
       return lerp(lerp(Hash(cell), Hash(cell + float2(1,0)), f.x),
           lerp(Hash(cell + float2(0,1)), Hash(cell + 1), f.x), f.y);
   }

   float GroundHeight(float2 p)
   {
       float2 uv = (p + 128) / 256;
       float inside = step(0, uv.x) * step(0, uv.y) * step(uv.x, 1) * step(uv.y, 1);
       float height = SAMPLE_TEXTURE2D_LOD(_IslandHeight, sampler_IslandHeight, uv, 0).r * 64 - 12;
       return lerp(-12, height, inside);
   }

   float3 WavePhase(float2 p, float time)
   {
       return float3(dot(p, Swell), dot(p, Crossing), dot(p, Chop)) - time * float3(.79, 1.18, 1.62);
   }

   float WaveAmplitude(float ground)
   {
       return _WaveHeight * lerp(1, 3.8, _SeaState.y) * lerp(.32, 1, smoothstep(.2, 5.5, _SeaState.x - ground));
   }

   Varyings Vert(Attributes input)
   {
       Varyings output;
       float3 world = TransformObjectToWorld(input.positionOS.xyz);
       float time = lerp(_Time.y, _SeaState.z, _SeaState.w);
       float3 phase = WavePhase(world.xz, time);
       world.y += _SeaState.x + WaveAmplitude(GroundHeight(world.xz)) * dot(sin(phase), float3(1,.48,.23));
       output.positionCS = TransformWorldToHClip(world);
       output.world = world;
       output.fog = ComputeFogFactor(output.positionCS.z);
       return output;
   }

   half4 Frag(Varyings input) : SV_Target
   {
       float hullDistance = 100;
       float wake = 0;
       float time = lerp(_Time.y, _SeaState.z, _SeaState.w);
       float storm = _SeaState.y;
       float2 p = input.world.xz;
       float noise = Noise(p * .76 + time * float2(-.22,.13));
       float fine = Noise(p * 2.7 + time * float2(.47,-.21));
       if (_HullRange.z > .5)
       {
           float3 hullPosition = mul(_HullWorldToLocal, float4(input.world,1)).xyz;
           float2 local = hullPosition.xz;
           float endDistance = max(_HullRange.x - local.y, local.y - _HullRange.y);
           if (endDistance < 2)
           {
               float sample = saturate((local.y - _HullRange.x) / (_HullRange.y - _HullRange.x)) * 16;
               int station = min((int)sample, 15);
               float2 sides = lerp(_HullWidths[station].xy, _HullWidths[station + 1].xy, sample - station);
               // The hull flares above the calm waterline and narrows toward the keel.
               float taper = smoothstep(0, 1.6, min(sample, 16 - sample));
               float flare = (.16 + .32 * clamp(hullPosition.y + 5.85, -2.0, 2.6)) * taper;
               sides += float2(-flare, flare);
               hullDistance = max(endDistance, max(sides.x - local.x, local.x - sides.y));
               clip(hullDistance);
           }
           float speed = abs(_HullRange.w);
           float aft = _HullRange.w >= 0 ? _HullRange.x - local.y : local.y - _HullRange.y;
           float length = lerp(4, 43, speed);
           float spread = 1.65 + aft * .24;
           float crest = 1 - smoothstep(.3, 1.65, abs(abs(local.x - 1.03) - spread + noise - .5));
           wake = step(0, aft) * (1 - smoothstep(1, length, aft)) * crest * speed * smoothstep(.26,.66,noise);
       }

       float ground = GroundHeight(p);
       float depth = max(0, input.world.y - ground);
       float shallow = exp(-depth * .28) * (1 - smoothstep(2.5, 6, depth));
       half3 color = lerp(_DeepColor.rgb, _ShallowColor.rgb, shallow);
       color = lerp(color, color * half3(.52,.63,.69), storm);
       color *= .89 + .19 * Noise(p * .084 - time * .022);
       float3 phase = WavePhase(p, time);
       float3 slopes = cos(phase);
       float crest = dot(sin(phase), float3(1,.48,.23));
       float2 gradient = WaveAmplitude(ground) * (slopes.x * Swell + .48 * slopes.y * Crossing + .23 * slopes.z * Chop);
       float distanceToView = distance(_WorldSpaceCameraPos, input.world);
       float rippleFade = 1 - smoothstep(45, 160, distanceToView);
       float2 ripple = float2(sin(dot(p,float2(3.1,1.35)) - time * 2.35 + noise * 2.8),
                             sin(dot(p,float2(-1.66,4.25)) - time * 3.1 + fine * 2.4));
       gradient += ripple * (.052 + storm * .042) * rippleFade;
       float3 normal = normalize(float3(-gradient.x, 1, -gradient.y));
       float3 view = normalize(GetWorldSpaceViewDir(input.world));
       Light sun = GetMainLight();
       float specular = pow(saturate(dot(reflect(-sun.direction,normal),view)), lerp(115,52,storm));
       float fresnel = pow(1 - saturate(dot(normal,view)), 4);
       // Directional light gives the moving slopes contrast even outside the sun glint.
       float slopeLight = dot(normal, sun.direction) - sun.direction.y;
       color *= clamp(1 + slopeLight * 1.15, .65, 1.35);
       color = lerp(color, _ShallowColor.rgb, smoothstep(.45, 1.45, crest) * (1 - shallow) * lerp(.085,.045,storm));
       color += specular * sun.color * lerp(.82,.24,storm) * lerp(.55,1.1,fine);
       color = lerp(color, lerp(half3(.4,.65,.74),half3(.32,.39,.44),storm), fresnel * .36);

       float shoreline = 1 - smoothstep(.05,.65 + noise * .52,depth);
       float breakers = 1 - smoothstep(.065,.31,abs(depth - (1.08 + .55 * sin(time * 1.12 + noise * 2))));
       float whitecaps = smoothstep(lerp(1.4,1.02,storm), lerp(1.68,1.54,storm), crest + noise * .27)
           * smoothstep(.4,.77,fine) * lerp(.32,1,storm);
       float hullFoam = (1 - smoothstep(.05,1.2,hullDistance)) * smoothstep(.29,.71,noise) * lerp(.15,.53,abs(_HullRange.w));
       float foam = saturate(shoreline * .55 + breakers * .33 + whitecaps * .72 + hullFoam + wake * .76);
       color = lerp(color, _FoamColor.rgb, foam * (.72 + fine * .25));
       return half4(MixFog(color,input.fog),1);
   }
   ENDHLSL
  }
 }
}
