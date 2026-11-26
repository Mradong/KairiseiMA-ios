import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const refs=path.join(root,'tools/csharp/unity-reference');
const out=path.join(root,'test-output/order05-reference-compile');fs.mkdirSync(out,{recursive:true});
const names=['UnityEngine.CoreModule.dll','UnityEngine.AssetBundleModule.dll','UnityEngine.IMGUIModule.dll','UnityEngine.UnityWebRequestModule.dll','UnityEngine.TextRenderingModule.dll','UnityEngine.JSONSerializeModule.dll','UnityEngine.InputLegacyModule.dll','UnityEngine.PhysicsModule.dll'];
const runtime=path.join(root,'ios-client/Assets/Recovery/Runtime');
const sources=fs.readdirSync(runtime).filter(n=>n.endsWith('.cs')).map(n=>path.join(runtime,n));
const compiler=path.join(root,'tools/csharp/toolset/tasks/net472/csc.exe');
const result=spawnSync(compiler,['/nologo','/utf8output','/target:library','/out:'+path.join(out,'Order05Runtime.dll'),'/reference:'+path.join(root,'tools/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll'),...names.map(n=>'/reference:'+path.join(refs,n)),...sources],{encoding:'utf8',windowsHide:true});
const hash=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const report={checkedAt:new Date().toISOString(),success:result.status===0,compiler:'Microsoft.Net.Compilers.Toolset 4.12.0',referenceVersion:'UnityEngine.Modules 2021.3.33',targetUnityVersion:'6000.3.26f1',
  stdout:result.stdout.trim(),stderr:result.stderr.trim(),sources:sources.map(p=>({file:path.relative(root,p).replaceAll('\\','/'),sha256:hash(p)})),
  referenceAssemblies:names.map(n=>({file:'tools/csharp/unity-reference/'+n,sha256:hash(path.join(refs,n))})),
  scope:'Auxiliary Windows C# source compilation against complete Unity 2021.3.33 API references. Original Unity 6000 client symbols are trimmed and cannot establish a complete API surface. This check does not establish exact Unity 6000 compatibility, Editor/IL2CPP/Xcode/shader builds, runtime rendering, iOS resources or device behavior. Reference DLLs are never executed or shipped.'};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/order05-runtime-reference-compilation.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify({success:report.success,referenceVersion:report.referenceVersion,stdout:report.stdout,stderr:report.stderr},null,2));if(!report.success)process.exitCode=1;
