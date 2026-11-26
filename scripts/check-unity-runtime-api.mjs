import fs from 'node:fs';
import path from 'node:path';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
import crypto from 'node:crypto';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const out=path.join(root,'test-output/ios-runtime-api');fs.mkdirSync(out,{recursive:true});
const symbols=path.join(root,'code-dump/DummyDll');
const referenceNames=['UnityEngine.CoreModule.dll','UnityEngine.AssetBundleModule.dll','UnityEngine.IMGUIModule.dll','UnityEngine.UnityWebRequestModule.dll','UnityEngine.TextRenderingModule.dll','UnityEngine.JSONSerializeModule.dll','UnityEngine.InputLegacyModule.dll','UnityEngine.PhysicsModule.dll','Il2CppDummyDll.dll'];
const references=referenceNames.map(f=>'/reference:'+path.join(symbols,f));
references.push('/reference:'+path.join(root,'tools/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll'));
const source=path.join(root,'ios-client/Assets/Recovery/Runtime');
const sources=fs.readdirSync(source).filter(f=>f.endsWith('.cs')).map(f=>path.join(source,f));
const modernCompiler=path.join(root,'tools/csharp/toolset/tasks/net472/csc.exe');
const compiler=fs.existsSync(modernCompiler)?modernCompiler:path.join(process.env.WINDIR||'C:/Windows','Microsoft.NET/Framework64/v4.0.30319/csc.exe');
const compile=spawnSync(compiler,['/nologo','/codepage:65001','/utf8output','/target:library','/out:'+path.join(out,'RecoveryRuntime.dll'),...references,...sources],{windowsHide:true});
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const report={generatedAt:new Date().toISOString(),success:compile.status===0,compiler:fs.existsSync(modernCompiler)?'Microsoft.Net.Compilers.Toolset 4.12.0':'Legacy .NET Framework csc',compilerSha256:hash(compiler),
 stdout:new TextDecoder('utf-8').decode(compile.stdout).trim(),stderr:new TextDecoder('utf-8').decode(compile.stderr).trim(),
 sourceFiles:sources.map(p=>({file:path.relative(root,p).replaceAll('\\','/'),sha256:hash(p)})),referenceFiles:referenceNames.map(f=>({file:'code-dump/DummyDll/'+f,sha256:hash(path.join(symbols,f))})),
 scope:'Runtime C# compile against recovered modern Unity public API metadata only. The referenced assemblies are dummy symbol assemblies and are never executed or uploaded. This is not Unity Editor, IL2CPP, Xcode, shader compilation, rendering or device validation.'};
for(const name of ['ios-runtime-api-check.json','order05-runtime-api-check.json']) fs.writeFileSync(path.join(root,'evidence/ios-package-search',name),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify({success:report.success,compiler:report.compiler,stdout:report.stdout,stderr:report.stderr,sourceFiles:report.sourceFiles.length},null,2));if(!report.success)process.exitCode=1;
