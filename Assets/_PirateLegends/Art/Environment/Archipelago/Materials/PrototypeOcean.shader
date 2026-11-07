Shader "PirateLegends/PrototypeOcean"
{
 Properties {
 _DeepColor("Deep water", Color)=(0.018,0.19,0.29,1)
 _ShallowColor("Shallow water", Color)=(0.08,0.68,0.63,1)
 _FoamColor("Sea foam", Color)=(0.84,0.97,0.91,1)
 _WaveHeight("Wave height", Range(0,1))=0.14
 _IslandHeight("Island elevation", 2D)="black" {}
 }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
 Pass {
 Tags { "LightMode"="UniversalForward" }
 Blend SrcAlpha OneMinusSrcAlpha
 ZWrite Off
 Cull Back
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
 TEXTURE2D(_IslandHeight);SAMPLER(sampler_IslandHeight);
 CBUFFER_START(UnityPerMaterial)
 half4 _DeepColor, _ShallowColor, _FoamColor;
 float _WaveHeight;
 CBUFFER_END
 struct Attributes { float4 positionOS:POSITION; };
 struct Varyings { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float eye:TEXCOORD1; half fog:TEXCOORD2; };
 float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
 float noise(float2 p) { float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y); }
 Varyings Vert(Attributes v) {
 Varyings o;
 float3 w=TransformObjectToWorld(v.positionOS.xyz);
 w.y+=_WaveHeight*(sin(w.x*.13+_Time.y*.8)+.45*sin(w.z*.21-_Time.y*1.1));
 o.positionCS=TransformWorldToHClip(w);o.world=w;o.eye=-TransformWorldToView(w).z;o.fog=ComputeFogFactor(o.positionCS.z);return o;
 }
 half4 Frag(Varyings i):SV_Target {
 float2 islandUV=(i.world.xz+128)/256;
 float inside=step(0,islandUV.x)*step(0,islandUV.y)*step(islandUV.x,1)*step(islandUV.y,1);
 float groundHeight=SAMPLE_TEXTURE2D(_IslandHeight,sampler_IslandHeight,islandUV).r*64-12;
 float depth=max(0,i.world.y-lerp(-12,groundHeight,inside));
 float n=noise(i.world.xz*.65+float2(_Time.y*.15,-_Time.y*.11));
 float broad=noise(i.world.xz*.045+_Time.y*.015);
 float shallow=exp(-depth*.28)*(1-smoothstep(2.5,6,depth));
 half3 col=lerp(_DeepColor.rgb,_ShallowColor.rgb,shallow);
 col*=.92+.13*broad;
 float phase=i.world.x*.7+i.world.z*.45+_Time.y*1.4;
 float ripples=pow(saturate(sin(phase+n*3)),18)*.012*(.2+.8*n);
 float2 deriv=float2(.13*cos(i.world.x*.13+_Time.y*.8),.0945*cos(i.world.z*.21-_Time.y*1.1))*_WaveHeight;
 float3 normal=normalize(float3(-deriv.x,1,-deriv.y));
 float3 view=normalize(GetWorldSpaceViewDir(i.world));
 Light sun=GetMainLight();
 float spec=pow(saturate(dot(reflect(-sun.direction,normal),view)),160);
 float fresnel=pow(1-saturate(dot(normal,view)),4);
 col+=ripples+spec*sun.color*.85;
 col=lerp(col,half3(.40,.65,.74),fresnel*.40);
 float edge=1-smoothstep(.04,.7+n*.6,depth);
 float surf=1-smoothstep(.05,.28,abs(depth-(1.1+.45*sin(_Time.y*.65+n*2))));
 float foam=saturate(edge*.8+surf*.35)*(0.5+.5*n);
 col=lerp(col,_FoamColor.rgb,foam);
 col=MixFog(col,i.fog);
 return half4(col,1);
 }
 ENDHLSL
 }
 }
}