import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const out=path.join(root,'test-output/order03-role-alias-differential');fs.mkdirSync(out,{recursive:true});
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const original=path.join(root,'inputs/native/kairisei_battlecore.dll'),rebuilt=path.join(root,'native/build/windows-amd64/kairisei_battlecore_rebuilt.dll');
const expected=JSON.parse(fs.readFileSync(path.join(root,'evidence/native/go-symbols-summary.json'),'utf8')).sha256;if(sha(original)!==expected)throw Error('Original DLL hash differs');
const schema=JSON.parse(fs.readFileSync(path.join(root,'evidence/ios-package-search/order03-modern-enum-differential.json'),'utf8')).enumCandidates;
const enums=Object.entries(schema).map(([name,v])=>({name,values:Object.entries(v.values).map(([name,value])=>({name,value}))}));
const fixture=JSON.parse(fs.readFileSync(path.join(root,'tests/fixtures/native-session/tables.json'),'utf8'));
const exe=path.join(out,'Order03RoleAliasProbe.exe'),source=path.join(root,'tests/Order03RoleAliasProbe.cs');
const csc=path.join(process.env.WINDIR,'Microsoft.NET/Framework64/v4.0.30319/csc.exe');const compile=spawnSync(csc,['/nologo','/platform:x64','/out:'+exe,source],{encoding:'utf8',windowsHide:true});if(compile.status!==0)throw Error(compile.stdout+compile.stderr);
const cases=[];
for(const [rule,sourceEnum,labels] of [['DOT','SKILL_ROLE_DOT',['NULL','POISON']],['TRANCE_GAUGE_STATE','ENEMY_TRANCE_GAUGE_STATE',['NONE','NORMAL']]]){
 const enumFile=path.join(root,'recovered-unity/ExportedProject/Assets/Scripts/Assembly-CSharp',sourceEnum+'.cs'),txt=fs.readFileSync(enumFile,'utf8');const values=labels.map(name=>{const m=txt.match(new RegExp('^\\s*'+name+'\\s*=\\s*(-?\\d+)\\s*,?\\s*$','m'));if(!m)throw Error('missing enum '+name);return {name,value:Number(m[1])};});
 for(const schemaName of [rule,sourceEnum]){
  const data=JSON.parse(JSON.stringify(fixture)),table=data.tables.find(t=>t.name==='skill_role_param_rule.csv');const old='ATTACK_AA,100,0,VALUE,';if(!table.text.includes(old))throw Error('fixture rule changed');table.text=table.text.replace(old,'ATTACK_AA,100,0,'+rule+',');data.enums=[...enums,{name:schemaName,values}];const input=path.join(out,rule+'-'+schemaName+'.json');fs.writeFileSync(input,JSON.stringify(data));
  const run=spawnSync(exe,[original,rebuilt,input],{encoding:'utf8',timeout:120000,windowsHide:true});if(run.status!==0)throw Error('Differential failed '+rule+'/'+schemaName+': '+run.stderr+' '+run.stdout);cases.push({rule,schemaName,sourceEnumSha256:sha(enumFile),result:JSON.parse(run.stdout.trim())});
 }
}
const report={checkedAt:new Date().toISOString(),completed:sha(original)===expected,allMatched:cases.every(c=>c.result.originalStatus===c.result.rebuiltStatus&&c.result.originalResult===c.result.rebuiltResult),originalSha256:sha(original),rebuiltSha256:sha(rebuilt),probeSha256:sha(source),fixtureSha256:sha(path.join(root,'tests/fixtures/native-session/tables.json')),schemaEvidenceSha256:sha(path.join(root,'evidence/ios-package-search/order03-modern-enum-differential.json')),bridgeSourceSha256:sha(path.join(root,'native/bridge/registered_skill.go')),cases,scope:'Windows x64 original versus rebuilt DLL on synthetic single registered skill; role parameter rule and enum names are varied. Query status/index only, no actual effect execution, supplied real tables, version matching, Unity/iOS or device.'};
report.expectedOutcomes=cases.every(c=>c.result.originalStatus===(c.schemaName===c.rule?2:0)&&c.result.originalResult===(c.schemaName===c.rule?12345:0));
fs.writeFileSync(path.join(root,'evidence/ios-package-search/order03-role-alias-differential.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report,null,2));if(!report.completed||!report.allMatched||!report.expectedOutcomes)process.exitCode=1;
