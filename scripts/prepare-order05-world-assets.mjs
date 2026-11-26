import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const sourceRoot='test-output/order05-home-pc-export/ExportedProject/Assets';
const targetRoot='ios-client/Assets/Recovery/OriginalWorldContent';
const guids=new Map(),copied=new Map(),shaderReconstructions=[];
const read=p=>fs.readFileSync(path.join(root,p));
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
function index(relative){for(const item of fs.readdirSync(path.join(root,relative),{withFileTypes:true})){const p=relative+'/'+item.name;if(item.isDirectory())index(p);else if(p.endsWith('.meta')){const guid=read(p).toString('utf8').match(/^guid:\s*([a-f0-9]{32})/m)?.[1];if(guid)guids.set(guid,p.slice(0,-5));}}}
index(sourceRoot);
function shaderSource(name,transparent,lightmap){return `Shader "${name}" {
Properties { _MainTex ("Original texture",2D)="white"{} _Color ("Original material color",Color)=(1,1,1,1) _LightMap ("Original light map",2D)="white"{} }
SubShader { Tags { "RenderType"="${transparent?'Transparent':'Opaque'}" "Queue"="${transparent?'Transparent':'Geometry'}" }
${transparent?'Blend SrcAlpha OneMinusSrcAlpha ZWrite Off':''} Cull Off
Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct Input {float4 vertex:POSITION;float2 uv:TEXCOORD0;float2 uv2:TEXCOORD1;fixed4 color:COLOR;};
struct Output {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float2 uv2:TEXCOORD1;fixed4 color:COLOR;};
sampler2D _MainTex,_LightMap; float4 _MainTex_ST; fixed4 _Color;
Output vert(Input i){Output o;o.vertex=UnityObjectToClipPos(i.vertex);o.uv=TRANSFORM_TEX(i.uv,_MainTex);o.uv2=i.uv2;o.color=i.color;return o;}
fixed4 frag(Output i):SV_Target {fixed4 c=tex2D(_MainTex,i.uv)*_Color;${lightmap?'c.rgb*=tex2D(_LightMap,i.uv2).rgb;':''}return c;}
ENDCG } } }
`;}
function copy(relative){if(copied.has(relative))return;const original=read(relative);let data=original;
 const rel=path.posix.relative(sourceRoot,relative);assert(!rel.startsWith('..'));
 const destination=targetRoot+'/'+rel;copied.set(relative,{source:relative,destination,sourceSha256:hash(original),bytes:original.length});
 if(relative.endsWith('.prefab')){
   let text=original.toString('utf8');const removed=[];
   text=text.split(/^--- !u!/m).map((block,i)=>{if(i===0)return block;const match=block.match(/^(\d+) &(-?\d+)/);if(match?.[1]==='114'){removed.push(match[2]);return '';}return '--- !u!'+block;}).join('');
   for(const id of removed)text=text.replace(new RegExp('^  - component: \\{fileID: '+id+'\\}\\r?\\n','gm'),'');
   data=Buffer.from(text);copied.get(relative).unrecoveredMonoBehavioursRemoved=removed.length;
 }
 if(relative.endsWith('.shader')){
   const text=original.toString('utf8'),name=text.match(/^Shader "([^"]+)"/)?.[1];assert(name);
   if(text.includes('DummyShaderTextExporter')){
     data=Buffer.from(shaderSource(name,/TexAlpha|Transparent/.test(name),/Lightmap/.test(name)));
     shaderReconstructions.push({source:relative,name,reason:'Exporter only emitted a dummy shader; portable texture/color/optional-lightmap implementation reconstructed. Exact original shader behavior/rendering remains unverified.'});
   }
 }
 fs.mkdirSync(path.dirname(path.join(root,destination)),{recursive:true});fs.writeFileSync(path.join(root,destination),data);
 if(fs.existsSync(path.join(root,relative+'.meta')))fs.copyFileSync(path.join(root,relative+'.meta'),path.join(root,destination+'.meta'));
 copied.get(relative).outputSha256=hash(data);
 if(relative.endsWith('.png')||relative.endsWith('.bytes'))return;
 for(const match of data.toString('utf8').matchAll(/guid:\s*([a-f0-9]{32})/g)){
   if(/^0{16}/.test(match[1]))continue;const dependency=guids.get(match[1]);
   assert(dependency,`Missing native-world dependency ${relative}: ${match[1]}`);copy(dependency);
 }
}
const prefab=sourceRoot+'/resources/02_map_assets/home/home_01000/hm_01000.prefab';copy(prefab);
const liveRoot=sourceRoot+'/resources/live2d/uathach';
function collect(relative){for(const item of fs.readdirSync(path.join(root,relative),{withFileTypes:true})){const p=relative+'/'+item.name;if(item.isDirectory())collect(p);else if(!p.endsWith('.meta'))copy(p);}}
collect(liveRoot);
const modelPath=liveRoot+'/uathach.model.json.bytes';
const modelRaw=read(modelPath).toString('utf8').replace(/^\uFEFF/,'');
let modelNormalized='',quoted=false,escaped=false;
for(let i=0;i<modelRaw.length;i++){
 const char=modelRaw[i];
 if(quoted){modelNormalized+=char;if(escaped)escaped=false;else if(char==='\\')escaped=true;else if(char==='"')quoted=false;continue;}
 if(char==='"')quoted=true;
 if(char===','){let next=i+1;while(/\s/.test(modelRaw[next]??''))next++;if(modelRaw[next]==='}'||modelRaw[next]===']')continue;}
 modelNormalized+=char;
}
const model=JSON.parse(modelNormalized);
const bindings=[];
for(const [source,record] of copied){let address;
 if(source===prefab)address='home-background-01000';
 else if(source.startsWith(liveRoot+'/'))address=path.posix.relative(sourceRoot+'/resources',source).replace(/\.bytes$/,'').replace(/\.png$/,'').toLowerCase();
 else continue;
 bindings.push({asset:record.destination.replace(/^ios-client\//,''),address});
}
const descriptor={schemaVersion:1,backgroundAsset:'home-background-01000',naviType:0,modelDefinition:'live2d/uathach/uathach.model.json',model,
  addressableAssets:bindings,shaderReconstructions,resourceFilesPrepared:true,renderingVerified:false,iOSCompiled:false};
fs.mkdirSync(path.join(root,targetRoot),{recursive:true});fs.writeFileSync(path.join(root,targetRoot,'OriginalWorld.json'),JSON.stringify(descriptor,null,2)+'\n');
const report={preparedAt:new Date().toISOString(),sourceVersion:'Exact received 2.0.4 PC-platform resource hashes verified by order05-scene-resource-import-validation-lyd.json',
  nativeBackgroundPrefab:prefab,naviModel:modelPath,files:[...copied.values()],addressableAssets:bindings.length,shaderReconstructions,
  originalFilesUnmodified:true,rawReferenceCodeOrBinariesCopied:false,resourceFilesPrepared:true,renderingVerified:false,iOSCompiled:false};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/order05-world-resource-preparation.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify({files:copied.size,addressableAssets:bindings.length,shaderReconstructions:shaderReconstructions.length,renderingVerified:false}));
