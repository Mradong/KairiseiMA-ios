import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const reference=path.join(root,'recovered-unity/ExportedProject/Assets/Scripts/Assembly-CSharp');
const out=path.join(root,'test-output/order03-modern-enum-differential');fs.mkdirSync(out,{recursive:true});
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const original=path.join(root,'inputs/native/kairisei_battlecore.dll');
const rebuilt=path.join(root,'native/build/windows-amd64/kairisei_battlecore_rebuilt.dll');
const expected=JSON.parse(fs.readFileSync(path.join(root,'evidence/native/go-symbols-summary.json'),'utf8')).sha256;
if(sha(original)!==expected)throw Error('Original DLL hash differs');
const families={
 SKILL_EXTEND_EXEC_ORDER:['NONE','SELF_MAIN_DECK_ATTR','SELF_MAIN_DECK_SKILL_KIND'],
 RARITY:['NULL','NORMAL'], SKILL_TYPE:['NORMAL'], SKILL_KIND:['NULL','ATTACK','RECOVERY'],
 ATTR:['NULL','FIRE','ICE'], JOB_TYPE:['NULL'], ARTHUR_TYPE:['NULL'],
 SKILL_PHYSICS_TYPE:['PHYSICS'], SKILL_TARGET:['SELF','ENEMY_ONE'], SKILL_EXEC_TIMING:['NORMAL'],
 SKILL_ROLE:['NULL','ATTACK_AA'], SKILL_ROLE_TARGET:['SELECT'], SKILL_ROLE_BATTLE_PARAM:['HP','ATK']
};
const chosen={};
for(const [family,labels] of Object.entries(families)){
 const p=path.join(reference,family+'.cs'), source=fs.readFileSync(p,'utf8');
 chosen[family]={sourceSha256:sha(p),values:{}};
 for(const label of labels){const match=source.match(new RegExp('^\\s*'+label+'\\s*=\\s*(-?\\d+)\\s*,?\\s*$','m'));if(!match)throw Error(`No explicit ${family}.${label}`);chosen[family].values[label]=Number(match[1]);}
}
const familyJson=Object.entries(chosen).map(([name,x])=>({name,values:Object.entries(x.values).map(([name,value])=>({name,value}))}));
let code=fs.readFileSync(path.join(root,'tests/RegisteredSkillQueryDifferential.cs'),'utf8');
function replaceOnce(old,value){if(!code.includes(old))throw Error('Cannot locate harness fragment: '+old);code=code.replace(old,value);}
const first=code.indexOf('  string[] families='), last=code.indexOf('  ulong ap=0,bp=0;');
if(first<0||last<0)throw Error('Harness structure changed');
const schema=JSON.stringify(familyJson).replaceAll('\\','\\\\').replaceAll('"','\\"');
code=code.slice(0,first)+`  text=text.Substring(0,text.Length-1)+",\\"enums\\":${schema}}";var tables=Encoding.UTF8.GetBytes(text);ulong ac=0,bc=0;if(a.open(tables,tables.Length,ref ac)!=0||b.open(tables,tables.Length,ref bc)!=0)throw new Exception("catalog");\n`+code.slice(last);
replaceOnce('foreach(int skillAttr in new[]{0,1})foreach(int skillKind in new[]{0,1})','foreach(int skillAttr in new[]{1,2})foreach(int skillKind in new[]{1,3})');
replaceOnce('foreach(int cond in new[]{1,2})foreach(int want in new[]{0,1})','foreach(int cond in new[]{35,36})foreach(int want in (cond==35?new[]{1,2}:new[]{1,3}))');
replaceOnce('Put(card,baseSkill+12,skillAttr);','Put(card,baseSkill+12,skillAttr);Put(card,baseSkill+44,3);');
replaceOnce('int checks=0,selected=0,missing=0;','int checks=0,selected=0,missing=0,rejected=0;');
replaceOnce('if(left==0&&x==0)selected++;if(left==0&&x==-1)missing++;checks++;','if(left==0&&x==0)selected++;else if(left==0&&x==-1)missing++;else if(left!=0)rejected++;else throw new Exception("unexpected successful result");checks++;');
const extra=code.indexOf('  foreach(int kind in new[]{0,1,2,3,4,5,6})'), end=code.indexOf('  if(selected==0||missing==0)');
if(extra<0||end<0)throw Error('Harness additional cases changed');code=code.slice(0,extra)+code.slice(end);
const src=path.join(out,'ModernEnumDifferential.cs');fs.writeFileSync(src,code);
const exe=path.join(out,'ModernEnumDifferential.exe');
const csc=path.join(process.env.WINDIR,'Microsoft.NET/Framework64/v4.0.30319/csc.exe');
const build=spawnSync(csc,['/nologo','/platform:x64','/out:'+exe,src],{encoding:'utf8',windowsHide:true});
if(build.status!==0)throw Error(build.stdout+build.stderr);
const run=spawnSync(exe,[original,rebuilt,path.join(root,'tests/fixtures/native-session/tables.json')],{encoding:'utf8',timeout:120000,windowsHide:true});
const report={checkedAt:new Date().toISOString(),success:run.status===0&&sha(original)===expected,counts:run.status===0?JSON.parse(run.stdout.trim().replace(/^\uFEFF/,'')):null,stderr:run.stderr.trim(),originalSha256:sha(original),rebuiltSha256:sha(rebuilt),harnessSha256:sha(path.join(root,'tests/RegisteredSkillQueryDifferential.cs')),fixtureSha256:sha(path.join(root,'tests/fixtures/native-session/tables.json')),enumCandidates:chosen,scope:'Windows synthetic prepared-card branch queries using selected enum values transcribed from modern recovered C# enums; original and rebuilt DLL compared. Failed queries are counted separately and agreement on rejection is not evidence of supported behavior. Enum source version compatibility with the supplied real tables, full schema, real cards, Unity/iOS and device are not proven.'};
if(report.counts)report.counts.rejected=report.counts.checks-report.counts.selected-report.counts.notSelected;
fs.writeFileSync(path.join(root,'evidence/ios-package-search/order03-modern-enum-differential.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report,null,2));if(!report.success)process.exitCode=1;
