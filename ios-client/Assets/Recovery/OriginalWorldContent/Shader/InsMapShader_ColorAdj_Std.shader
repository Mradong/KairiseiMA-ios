Shader "InsMapShader/ColorAdj_Std" {
Properties { _MainTex ("Original texture",2D)="white"{} _Color ("Original material color",Color)=(1,1,1,1) _LightMap ("Original light map",2D)="white"{} }
SubShader { Tags { "RenderType"="Opaque" "Queue"="Geometry" }
 Cull Off
Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct Input {float4 vertex:POSITION;float2 uv:TEXCOORD0;float2 uv2:TEXCOORD1;fixed4 color:COLOR;};
struct Output {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float2 uv2:TEXCOORD1;fixed4 color:COLOR;};
sampler2D _MainTex,_LightMap; float4 _MainTex_ST; fixed4 _Color;
Output vert(Input i){Output o;o.vertex=UnityObjectToClipPos(i.vertex);o.uv=TRANSFORM_TEX(i.uv,_MainTex);o.uv2=i.uv2;o.color=i.color;return o;}
fixed4 frag(Output i):SV_Target {fixed4 c=tex2D(_MainTex,i.uv)*_Color;return c;}
ENDCG } } }
