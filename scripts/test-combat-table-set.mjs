import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const out=path.join(root,'test-output/combat-tables');fs.mkdirSync(out,{recursive:true});
const json=path.join(root,'tools/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll');
fs.copyFileSync(json,path.join(out,'Newtonsoft.Json.dll'));
const exe=path.join(out,'CombatTableSetTests.exe');
const build=spawnSync(path.join(process.env.WINDIR,'Microsoft.NET/Framework64/v4.0.30319/csc.exe'),
 ['/nologo','/platform:x64','/out:'+exe,'/reference:'+json,path.join(root,'tests/CombatTableSetTests.cs'),path.join(root,'ios-client/Assets/Recovery/Runtime/CombatTableSet.cs')],{encoding:'utf8'});
if(build.status!==0)throw new Error(build.stdout+build.stderr);
const run=spawnSync(exe,[path.join(root,'tests/fixtures/native-session/tables.json')],{encoding:'utf8',timeout:60000});
const report={checkedAt:new Date().toISOString(),success:run.status===0,counts:run.status===0?JSON.parse(run.stdout.trim()):null,stderr:run.stderr.trim(),
 sources:['ios-client/Assets/Recovery/Runtime/CombatTableSet.cs','tests/CombatTableSetTests.cs'].map(file=>({file,sha256:crypto.createHash('sha256').update(fs.readFileSync(path.join(root,file))).digest('hex')})),
 actualCombatSnapshotPresent:fs.existsSync(path.join(root,'ios-client/Assets/Recovery/Combat/CombatTables.json')),
 scope:'Windows .NET execution of the actual runtime combat-resource transport validator using synthetic catalog tables. Missing masters, schema, duplicate/path names, partial optional groups, invalid JSON/UTF-8 and byte bounds are rejected. Native CSV semantics are separately exercised by registered-battle original-DLL tests. Unity resource compilation, loading, actual master data and device execution remain unverified.'};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/ios-combat-table-tests.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report,null,2));if(!report.success)process.exitCode=1;
