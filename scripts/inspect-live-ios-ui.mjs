import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const exported=path.resolve(root,process.argv[2]||'test-output/live-ios-service-ui-20261120');
if(!exported.startsWith(root+path.sep))throw new Error('UI inspection must stay in the recovery workspace.');
const live=JSON.parse(fs.readFileSync(path.join(root,'evidence/ios-package-search/ios-live-service-test.json'),'utf8'));
const reference=JSON.parse(fs.readFileSync(path.join(root,'evidence/ios-package-search/ios-client004-artifact-verification.json'),'utf8'));
if(!live.success||!reference.success||live.content.contentId!==reference.contentId)throw new Error('Live and reviewed 0.0.4 resource identities differ.');
const hash=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const imported=JSON.parse(fs.readFileSync(path.join(exported,'recovery-run.json'),'utf8'));
const ui=live.content.bundles.find(b=>b.name==='recovery/ui.dat');
if(!ui||hash(imported.input)!==ui.sha256||!reference.bundles.some(b=>b.sha256===ui.sha256)||imported.importErrors||imported.exportErrors)throw new Error('UI export does not match the verified public bundle.');
const assets=path.join(exported,'ExportedProject/Assets');
const images=['title-background.png','Title.png','Party.png','Battle.png','Deck_Battle.png','Cmn.png'].map(file=>{
 const p=path.join(assets,file),data=fs.readFileSync(p);
 if(!data.subarray(0,8).equals(Buffer.from([137,80,78,71,13,10,26,10])))throw new Error('Exported UI image is not PNG.');
 const width=data.readUInt32BE(16),height=data.readUInt32BE(20);if(width<1||height<1)throw new Error('Empty UI image.');
 return {file,width,height,sha256:hash(p)};
});
const metadata=['Party-frames.json','Battle-frames.json','Deck_Battle-frames.json','Cmn-frames.json'].map(file=>{
 const p=path.join(assets,file),value=JSON.parse(fs.readFileSync(p,'utf8'));
 if(!value.frames||!value.meta?.size)throw new Error('Exported atlas metadata is incomplete.');
 return {file,frameCount:Object.keys(value.frames).length,sha256:hash(p)};
});
const out=path.join(root,'test-output/live-ui-inspection');fs.mkdirSync(out,{recursive:true});
const json=path.join(root,'tools/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll');fs.copyFileSync(json,path.join(out,'Newtonsoft.Json.dll'));
const exe=path.join(out,'RecoveryAtlasTests.exe');
const build=spawnSync(path.join(process.env.WINDIR,'Microsoft.NET/Framework64/v4.0.30319/csc.exe'),['/nologo','/reference:'+json,'/reference:System.Drawing.dll','/out:'+exe,path.join(root,'ios-client/Assets/Recovery/Runtime/RecoveryAtlas.cs'),path.join(root,'tests/RecoveryAtlasTests.cs')],{encoding:'utf8'});
if(build.status!==0)throw new Error(build.stdout+build.stderr);
const run=spawnSync(exe,[path.join(assets,'Party-frames.json'),path.join(assets,'Party.png')],{encoding:'utf8'});
const report={checkedAt:new Date().toISOString(),success:run.status===0,contentId:live.content.contentId,bundleSha256:ui.sha256,matchesReviewed004Bundle:true,importErrors:imported.importErrors,exportErrors:imported.exportErrors,images,metadata,actualRuntimeAtlasChecks:run.stdout.trim(),stderr:run.stderr.trim(),
 scope:'AssetRipper statically imports and decodes the exact publicly downloaded iOS bundle; recovered PNG and atlas JSON inspected, actual C# atlas parser tested against downloaded data. This does not execute Unity AssetBundle.LoadAsset, GPU rendering, UnityWebRequest or the iPhone client.',unityRuntimeLoaded:false,deviceVerified:false};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/ios-live-ui-inspection.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report,null,2));if(!report.success)process.exitCode=1;
