import fs from 'node:fs';
import path from 'node:path';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const output=path.join(root,'test-output/ios-content',new Date().toISOString().replace(/[:.]/g,'-'));
fs.mkdirSync(output,{recursive:true});
const json=path.join(root,'tools/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll');
fs.copyFileSync(json,path.join(output,'Newtonsoft.Json.dll'));
const executable=path.join(output,'RecoveryContentTests.exe');
const compile=spawnSync(path.join(process.env.WINDIR||'C:/Windows','Microsoft.NET/Framework64/v4.0.30319/csc.exe'),['/nologo','/codepage:65001','/reference:'+json,'/out:'+executable,
  path.join(root,'ios-client/Assets/Recovery/Runtime/RecoveryContent.cs'),path.join(root,'ios-client/Assets/Recovery/Runtime/CnProtocol.cs'),path.join(root,'tests/RecoveryContentTests.cs')],{encoding:'utf8',windowsHide:true});
if(compile.status!==0)throw new Error(compile.stdout+compile.stderr);
const run=spawnSync(executable,[path.join(output,'cache-test')],{encoding:'utf8',windowsHide:true,timeout:60000});
const result={generatedAt:new Date().toISOString(),success:run.status===0,stdout:run.stdout.trim(),stderr:run.stderr.trim(),scope:'Actual resource manifest and persistent cache C# compiled and executed on Windows .NET Framework with byte fixtures. Does not validate Unity imports, iOS bundles, overwrite installation on phone, or network downloads.'};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/ios-content-tests.json'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result,null,2));
if(!result.success)process.exitCode=1;
