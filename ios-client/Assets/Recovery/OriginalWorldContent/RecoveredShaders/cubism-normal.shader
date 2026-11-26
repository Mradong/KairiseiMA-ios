Shader "Live2D/Live2D" {
Properties { _MainTex ("Original model texture",2D)="white"{} _ClipTex ("Original clipping mask",2D)="white"{} _ChannelFlag ("Clipping channel",Vector)=(1,0,0,0)
 _Cull ("Cull",Float)=0 _SrcColor ("Source color blend",Float)=5 _DstColor ("Destination color blend",Float)=10 _SrcAlpha ("Source alpha blend",Float)=1 _DstAlpha ("Destination alpha blend",Float)=10 }
SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull [_Cull] ZWrite Off
Blend [_SrcColor] [_DstColor], [_SrcAlpha] [_DstAlpha]
Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct Input {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
struct Output {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 mask:TEXCOORD1;fixed4 color:COLOR;};
sampler2D _MainTex,_ClipTex;float4x4 _MaskMatrix,_ClipMatrix;float4 _Bounds,_ChannelFlag;
Output vert(Input i){Output o;o.vertex=UnityObjectToClipPos(i.vertex);o.uv=i.uv;o.mask=mul(_ClipMatrix,i.vertex);o.color=i.color;return o;}
fixed4 frag(Output i):SV_Target {fixed4 c=tex2D(_MainTex,i.uv)*i.color;
return c; }
ENDCG } } }
