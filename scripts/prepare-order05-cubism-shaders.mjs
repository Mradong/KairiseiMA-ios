import fs from 'node:fs';import path from 'node:path';import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const dir='ios-client/Assets/Recovery/OriginalWorldContent/RecoveredShaders';fs.mkdirSync(path.join(root,dir),{recursive:true});
const specs=[
 ['Live2D/Live2D','cubism-normal',false,false,false],
 ['Live2D/Live2D Masked','cubism-normal-masked',true,false,false],
 ['Live2D/Live2D PremultipliedAlpha','cubism-premultiplied',false,true,false],
 ['Live2D/Live2D Masked PremultipliedAlpha','cubism-premultiplied-masked',true,true,false],
 ['Live2D/SetupMask','cubism-setup-mask',false,false,true]
];
for(const [name,address,masked,premultiplied,setup] of specs){
 const source=`Shader "${name}" {
Properties { _MainTex ("Original model texture",2D)="white"{} _ClipTex ("Original clipping mask",2D)="white"{} _ChannelFlag ("Clipping channel",Vector)=(1,0,0,0)
 _Cull ("Cull",Float)=0 _SrcColor ("Source color blend",Float)=${premultiplied?1:5} _DstColor ("Destination color blend",Float)=10 _SrcAlpha ("Source alpha blend",Float)=1 _DstAlpha ("Destination alpha blend",Float)=10 }
SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull [_Cull] ZWrite Off
${setup?'Blend Zero OneMinusSrcColor':'Blend [_SrcColor] [_DstColor], [_SrcAlpha] [_DstAlpha]'}
Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct Input {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
struct Output {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 mask:TEXCOORD1;fixed4 color:COLOR;};
sampler2D _MainTex,_ClipTex;float4x4 _MaskMatrix,_ClipMatrix;float4 _Bounds,_ChannelFlag;
Output vert(Input i){Output o;o.vertex=${setup?'mul(_MaskMatrix,i.vertex)':'UnityObjectToClipPos(i.vertex)'};o.uv=i.uv;o.mask=${setup?'o.vertex':'mul(_ClipMatrix,i.vertex)'};o.color=i.color;return o;}
fixed4 frag(Output i):SV_Target {fixed4 c=tex2D(_MainTex,i.uv)*i.color;
${setup?'float2 p=i.mask.xy/i.mask.w;clip(p-_Bounds.xy);clip(_Bounds.zw-p);return _ChannelFlag*c.a;':masked?'float2 p=i.mask.xy/i.mask.w;float mask=dot(1-tex2D(_ClipTex,p),_ChannelFlag);c*=mask;return c;':'return c;'} }
ENDCG } } }
`;
 fs.writeFileSync(path.join(root,dir,address+'.shader'),source);
}
const descriptorPath=path.join(root,'ios-client/Assets/Recovery/OriginalWorldContent/OriginalWorld.json');
const data=JSON.parse(fs.readFileSync(descriptorPath,'utf8'));
data.shaderAssets=specs.map(([name,address])=>({originalName:name,address,asset:dir.replace(/^ios-client\//,'')+'/'+address+'.shader'}));
data.shaderCompatibility='Reconstructed standard Cubism 2.1 texture, premultiplication, blend and RGBA clipping branches using the recovered SDK uniform contract. Shader compilation/GPU appearance unverified; HSL/ColorList remain explicitly unsupported.';
fs.writeFileSync(descriptorPath,JSON.stringify(data,null,2)+'\n');
console.log('Prepared five explicit Cubism shader branches; original assets and shader-runtime limits retained.');
