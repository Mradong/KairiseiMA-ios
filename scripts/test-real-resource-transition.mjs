import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const live=JSON.parse(fs.readFileSync(path.join(root,'evidence/ios-package-search/ios-live-service-test.json'),'utf8'));
const delivery=JSON.parse(fs.readFileSync(path.join(root,'evidence/ios-package-search/ipa-client006-delivery.json'),'utf8'));
if(!live.success || !delivery.embeddedResourceValidation.success || delivery.sourceCommit!=='692ef3939dfeb0278f43d8bffee2871dfbdd0dd6')throw new Error('Verified source reports are required.');
const embeddedDir=path.join(root,'dist/cloud-artifacts/ios-client006-44704875254/resources');
const serverDir=path.join(root,live.publicResourceDirectory);
const embedded=delivery.embeddedResourceValidation.bundles[0],server=live.content.bundles[0];
const input=[path.join(embeddedDir,'client-manifest.json'),path.join(embeddedDir,embedded.file),path.join(serverDir,'client-manifest.json'),path.join(serverDir,'cache',server.sha256+'.bundle')];
const hash=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
if(hash(input[1])!==embedded.sha256 || hash(input[3])!==server.sha256)throw new Error('Actual bundle source hashes differ.');
const out=path.join(root,'test-output/real-resource-transition',new Date().toISOString().replace(/[:.]/g,'-'));
fs.mkdirSync(out,{recursive:true});
const json=path.join(root,'tools/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll');fs.copyFileSync(json,path.join(out,'Newtonsoft.Json.dll'));
const exe=path.join(out,'RealResourceTransitionTests.exe');
const sources=['ios-client/Assets/Recovery/Runtime/CnProtocol.cs','ios-client/Assets/Recovery/Runtime/RecoveryContent.cs','tests/RealResourceTransitionTests.cs'];
const compile=spawnSync(path.join(process.env.WINDIR,'Microsoft.NET/Framework64/v4.0.30319/csc.exe'),['/nologo','/platform:x64','/reference:'+json,'/out:'+exe,...sources.map(f=>path.join(root,f))],{encoding:'utf8'});
if(compile.status!==0)throw new Error(compile.stdout+compile.stderr);
const run=spawnSync(exe,[...input,path.join(out,'cache')],{encoding:'utf8',timeout:60000});
const report={checkedAt:new Date().toISOString(),success:run.status===0,stdout:run.stdout.trim(),stderr:run.stderr.trim(),
 sourceCommit:delivery.sourceCommit,embeddedContentId:delivery.embeddedResourceValidation.contentId,embeddedBundleSha256:embedded.sha256,serverContentId:live.content.contentId,serverBundleSha256:server.sha256,
 sourceHashes:sources.map(file=>({file,sha256:hash(path.join(root,file))})),
 scope:'Windows .NET execution of the actual 0.0.6 content manifest and cache code against its actual embedded iOS bundle and the independently downloaded public-service iOS bundle. Tests preserve both versions during a truncated or corrupt staged update and after cache reopen. Does not exercise Unity AssetBundle loading, UnityWebRequest, iPhone filesystem, installed app overwrite or real gameplay.'};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/ios-real-resource-transition-tests.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report,null,2));if(!report.success)process.exitCode=1;
