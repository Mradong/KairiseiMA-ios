import fs from 'node:fs';import path from 'node:path';import crypto from 'node:crypto';import assert from 'node:assert/strict';import {spawnSync} from 'node:child_process';import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const out=fs.mkdtempSync(path.join(root,'test-output/order05-cubism-geometry-'));
const refs=path.join(root,'tools/csharp/unity-reference');
const model='test-output/order05-home-pc-export/ExportedProject/Assets/resources/live2d/uathach/moc/uathach.moc.bytes';
const template=fs.readFileSync(path.join(root,'tests/CubismModelGeometryProbe.cs'),'utf8');
const reports=[];
for(const [kind,namespace] of [['original','live2d'],['rebuilt','Kairisei.Recovery.Cubism2']]){
 const dir=path.join(out,kind);fs.mkdirSync(dir,{recursive:true});
 for(const name of fs.readdirSync(refs).filter(n=>n.endsWith('.dll')))fs.copyFileSync(path.join(refs,name),path.join(dir,name));
 const source=path.join(dir,'Probe.cs');fs.writeFileSync(source,template.replace('SDK_NAMESPACE',namespace));
 const extra=kind==='original'?['/reference:'+path.join(root,'legacy-managed/Live2DUnity.dll')]:[path.join(root,'ios-client/Assets/Recovery/Runtime/RecoveredCubism2.cs'),path.join(root,'ios-client/Assets/Recovery/Runtime/OriginalLive2DShaders.cs')];
 const exe=path.join(dir,'Probe.exe');
 const build=spawnSync(path.join(root,'tools/csharp/toolset/tasks/net472/csc.exe'),['/nologo','/utf8output','/out:'+exe,...fs.readdirSync(refs).filter(n=>n.endsWith('.dll')).map(n=>'/reference:'+path.join(refs,n)),...extra,source],{encoding:'utf8',windowsHide:true});
 assert.equal(build.status,0,build.stdout+build.stderr);
 if(kind==='original')fs.copyFileSync(path.join(root,'legacy-managed/Live2DUnity.dll'),path.join(dir,'Live2DUnity.dll'));
 const run=spawnSync(exe,[path.join(root,model)],{encoding:'utf8',windowsHide:true,timeout:30000});
 reports.push({kind,success:run.status===0,stdout:run.stdout.trim(),stderr:run.stderr.trim()});
}
const success=reports.every(r=>r.success)&&reports[0].stdout===reports[1].stdout;
const report={checkedAt:new Date().toISOString(),success,reports,model,modelSha256:crypto.createHash('sha256').update(fs.readFileSync(path.join(root,model))).digest('hex'),
 scope:'Windows .NET CPU-only original managed SDK versus rebuilt Cubism source, real received uathach MOC, 27 actual pose/eye/mouth states; all deformed float arrays from each real draw context serialized using original member identity and compared exactly. No Unity draw method is invoked; clipping disabled only in this probe. Does not establish masks, shading, GPU/Unity rendering, iOS/ARM64 or device behavior.'};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/order05-cubism-geometry-differential.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report,null,2));if(!success)process.exitCode=1;
