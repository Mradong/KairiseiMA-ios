import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const out = path.join(root,'test-output/order05-entry-flow',new Date().toISOString().replace(/[:.]/g,'-'));
fs.mkdirSync(out,{recursive:true});
const dll = path.join(root,'tools/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll');
fs.copyFileSync(dll,path.join(out,'Newtonsoft.Json.dll'));
const sources = ['tests/Order05EntryFlowTests.cs','ios-client/Assets/Recovery/Runtime/CnProtocol.cs','ios-client/Assets/Recovery/Runtime/CnEntryFlow.cs','ios-client/Assets/Recovery/Runtime/CnEntryJournal.cs'];
const exe = path.join(out,'Order05EntryFlowTests.exe');
const build = spawnSync(path.join(process.env.WINDIR,'Microsoft.NET/Framework64/v4.0.30319/csc.exe'),['/nologo','/codepage:65001','/out:'+exe,'/reference:'+dll,...sources.map(p=>path.join(root,p))],{encoding:'utf8',windowsHide:true});
if(build.status!==0) throw new Error(build.stdout+build.stderr);
const run = spawnSync(exe,[out],{encoding:'utf8',windowsHide:true});
const report = {checkedAt:new Date().toISOString(),success:run.status===0,stdout:run.stdout.trim(),stderr:run.stderr.trim(),
  sources:sources.map(file=>({file,sha256:crypto.createHash('sha256').update(fs.readFileSync(path.join(root,file))).digest('hex')})),
  scope:'Windows .NET actual CnEntryFlow/CnProtocol/CnEntryJournal with synthetic responses and workspace-only persistent markers. Checks ambiguous create, duplicate operations, resource retry, HTTP-200/401 expired sessions, wrong home identity, previous home retention and pending-battle guard. Does not execute RecoveryBootstrap coroutines, Unity networking/rendering, actual service, iOS/ARM64 or device.'};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/order05-entry-flow-tests.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report,null,2));
if(!report.success)process.exitCode=1;
