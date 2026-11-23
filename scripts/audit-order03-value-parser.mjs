import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const out=path.join(root,'test-output/order03-value-parser');fs.mkdirSync(out,{recursive:true});
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const legacy='legacy-unity/ExportedProject/Assets/Scripts/Assembly-CSharp/';
const skill=path.join(root,legacy,'SkillDataContainer.cs'),global=path.join(root,legacy,'Global.cs');
const audit=path.join(root,'inputs/server-order03/Order03-Combat-Verified-20261123/audit/raw-role-parameters.json');
const goSource=path.join(root,'source-review/kairisei-ma-ch/server/battlecontent/skill_programs.go');
function method(text,signature){let start=text.indexOf(signature);if(start<0||text.indexOf(signature,start+1)>=0)throw Error('method signature');let open=text.indexOf('{',start),depth=0;for(let i=open;i<text.length;i++){if(text[i]==='{')depth++;if(text[i]==='}'&&--depth===0)return text.slice(start,i+1);}throw Error('method boundary');}
const getRole=method(fs.readFileSync(skill,'utf8'),'private int getRoleParam(');
const match=getRole.match(/case SKILL_ROLE_PARAM\.VALUE:\s*([\s\S]*?)\s*case SKILL_ROLE_PARAM\.SKILL_KIND:/);
if(!match||!match[1].includes('Global.parseInt(str)'))throw Error('VALUE branch changed');
const gt=fs.readFileSync(global,'utf8');const parseOut=method(gt,'public static int parseInt(out int o, string s, int def = 0)'),parse=method(gt,'public static int parseInt(string s, int def = 0)');
const cs=`using System; class Global { ${parseOut}\n${parse} } class Probe { static int Value(string str) { switch (1) { case 1: ${match[1]} } } static int Main(string[] args) { foreach (var x in args) Console.WriteLine(Value(x)); return 0; } }`;
const csPath=path.join(out,'Probe.cs'),exe=path.join(out,'Probe.exe');fs.writeFileSync(csPath,cs);
function run(command,args,options={}){const r=spawnSync(command,args,{encoding:'utf8',windowsHide:true,timeout:120000,...options});if(r.status!==0)throw Error(command+' failed: '+r.stderr+' '+r.stdout);return r.stdout.trim().split(/\r?\n/);}
run(path.join(process.env.WINDIR,'Microsoft.NET/Framework64/v4.0.30319/csc.exe'),['/nologo','/platform:x64','/out:'+exe,csPath]);
const mod=`module order03valueprobe\n\ngo 1.25.0\n\nrequire kairisei.local/server v0.0.0\n\nreplace kairisei.local/server => ${path.join(root,'source-review/kairisei-ma-ch/server').replaceAll('\\','/')}\n`;
fs.writeFileSync(path.join(out,'go.mod'),mod);
fs.writeFileSync(path.join(out,'main.go'),`package main\nimport("fmt";"os";"kairisei.local/server/battlecontent";"kairisei.local/server/battlemodel")\nfunc main(){for _,s:=range os.Args[1:]{r:=battlemodel.CombatSkillRole{Function:"PROBE"};r.Parameters[0]=s;rule:=battlemodel.CombatRoleParameterRule{};rule.Parameters[0]="VALUE";v,e:=battlecontent.BindSkillRole(r,map[string]battlemodel.CombatRoleParameterRule{"PROBE":rule});if e!=nil{panic(e)};fmt.Println(v.Parameters[0])}}\n`);
const goExe=path.join(out,'go-probe.exe'),goEnv={...process.env,GOROOT:path.join(root,'tools/go'),GOPATH:path.join(root,'tools/go-work'),GOMODCACHE:path.join(root,'tools/go-module-cache'),GOCACHE:path.join(root,'tools/go-build-cache'),GOTMPDIR:out,GOTOOLCHAIN:'local',CGO_ENABLED:'0'};run(path.join(root,'tools/go/bin/go.exe'),['build','-o',goExe,'.'],{cwd:out,env:goEnv});
const observations=JSON.parse(fs.readFileSync(audit,'utf8')).numeric_problems;if(observations.length!==25)throw Error('unexpected observations');
const controls=['ALL','2147483647','-2147483648','2147483648','-2147483649','0'];const inputs=[...observations.map(x=>x.raw_value),...controls];
const old=run(exe,inputs),rebuilt=run(goExe,inputs);if(old.length!==inputs.length||rebuilt.length!==inputs.length)throw Error('output count');
const cases=inputs.map((value,i)=>({value,legacyValue:Number(old[i]),goValue:Number(rebuilt[i]),source:i<observations.length?'delivered_observation':'control'}));
const report={checkedAt:new Date().toISOString(),sourceHashes:{legacySkill:sha(skill),legacyGlobal:sha(global),deliveryAudit:sha(audit),goBinder:sha(goSource),script:sha(fileURLToPath(import.meta.url))},observations:observations.length,controls:controls.length,matched:cases.filter(x=>x.legacyValue===x.goValue).length,observedAllZero:cases.slice(0,observations.length).every(x=>x.legacyValue===0&&x.goValue===0),cases,scope:'Windows x64: exact VALUE branch extracted from readable 1.4.0 client source and exact Global.parseInt methods compiled in a minimal harness, compared with actual Go BindSkillRole used by the rebuilt bridge. Raw inputs are the 25 audit observations plus controls. Not 6.0.2 proof, CSV-to-rule/record verification, actual skill execution, original native DLL behavior, ARM64 or device proof.'};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/order03-value-parser-differential.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report,null,2));if(report.matched!==inputs.length||!report.observedAllZero)process.exitCode=1;
