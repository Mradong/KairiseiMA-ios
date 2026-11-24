import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const out=path.join(root,'test-output/order03-enemy-flag');fs.mkdirSync(out,{recursive:true});
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const original=path.join(root,'inputs/native/kairisei_battlecore.dll'),rebuilt=path.join(root,'native/build/windows-amd64/kairisei_battlecore_rebuilt.dll'),source=path.join(root,'tests/Order03EnemyFlagProbe.cs'),fixture=path.join(root,'tests/fixtures/native-session/tables.json');
const expected=JSON.parse(fs.readFileSync(path.join(root,'evidence/native/go-symbols-summary.json'),'utf8')).sha256;if(sha(original)!==expected)throw Error('Original DLL hash differs');
const json=path.join(root,'tools/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll'),exe=path.join(out,'probe.exe'),raw=path.join(out,'results.json');fs.copyFileSync(json,path.join(out,'Newtonsoft.Json.dll'));
const build=spawnSync(path.join(process.env.WINDIR,'Microsoft.NET/Framework64/v4.0.30319/csc.exe'),['/nologo','/platform:x64','/out:'+exe,'/reference:'+json,source],{encoding:'utf8',windowsHide:true});if(build.status!==0)throw Error(build.stdout+build.stderr);
const run=spawnSync(exe,[original,rebuilt,fixture,raw],{encoding:'utf8',windowsHide:true,timeout:120000});if(run.status!==0)throw Error(run.stdout+run.stderr);
const cases=JSON.parse(fs.readFileSync(raw,'utf8'));let matched=0,selected=0,fallback=0;
for(const c of cases){
 if(c.original.createStatus!==0||c.original.turns.length!==3)throw Error('Original scenario failed: '+JSON.stringify({label:c.label,slot:c.slot,index:c.index,status:c.original.createStatus}));
 for(let turn=0;turn<3;turn++){
  const set=turn>0&&c.setFlag>0&&c.index<32;
  const one=(set&&c.first===c.index)||(turn>0&&c.secondFlag>=0&&c.first===c.secondFlag),two=(set&&c.second===c.index)||(turn>0&&c.secondFlag>=0&&c.second===c.secondFlag);
  const branch=c.label==='SELF_ENEMY_AI_FLAG'?one||two:one&&two;
  const damage=branch?13:7;
  const hits=c.original.turns[turn].filter(x=>x.startsWith('60,'));
  if(hits.length!==1||hits[0].split(',').slice(0,4).join(',')!==`60,5,1,-${damage}`)throw Error('Original branch outcome differs: '+JSON.stringify({label:c.label,turn,index:c.index,setFlag:c.setFlag,first:c.first,second:c.second,hits}));
  if(branch)selected++;else fallback++;
 }
 if(JSON.stringify(c.original)===JSON.stringify(c.rebuilt))matched++;
}
const observe=process.argv.includes('--observe');
const compact=x=>({createStatus:x.createStatus,turns:x.turns,traceSha256:crypto.createHash('sha256').update(JSON.stringify(x.trace)).digest('hex'),journalSha256:x.journal===undefined?null:crypto.createHash('sha256').update(x.journal).digest('hex')});
const report={checkedAt:new Date().toISOString(),success:cases.length===40&&matched===cases.length&&selected>0&&fallback>0&&sha(original)===expected,cases:cases.length,turns:selected+fallback,selected,fallback,fullTraceAndJournalMatches:matched,originalSha256:sha(original),rebuiltSha256:sha(rebuilt),probeSha256:sha(source),scriptSha256:sha(fileURLToPath(import.meta.url)),fixtureSha256:sha(fixture),bridgeSourceSha256:sha(path.join(root,'native/bridge/skill_query.go')),rawResultsSha256:sha(raw),results:cases.map(c=>({...c,original:compact(c.original),rebuilt:compact(c.rebuilt)})),scope:'Windows x64 synthetic registered enemy action over three turns. OR/AND flag conditions, two slots, bit 0/31/out-of-range, set and initially clear states and distinct simultaneously set flags; full result traces and journals compared. No set-then-clear transition, real tables or live service, complete enemy rules, Unity, ARM64 or device proof.'};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/order03-enemy-flag'+(observe?'-before-fix':'')+'.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({...report,results:undefined},null,2));if(!observe&&!report.success)process.exitCode=1;
