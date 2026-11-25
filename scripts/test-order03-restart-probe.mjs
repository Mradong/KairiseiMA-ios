import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {spawn} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const output=fs.mkdtempSync(path.join(root,'test-output/order03-restart-probe-'));
const script=process.env.ORDER03_TEST_SCRIPT||path.join(root,'scripts/Verify-Order03Restart.ps1');
const reproduction=JSON.parse(fs.readFileSync(path.join(root,'evidence/ios-package-search/order03-reviewed-patch-reproduction.json')));
let homeReads=0,startReads=0;
const opening={seed:123,start_time:456,battles:[{enemy_partyid:30010102,enemy_type:1}],cost_initial:3,hold_max:6,partner_deck:[]};
const server=http.createServer((req,res)=>{
  let text='';req.on('data',chunk=>text+=chunk);req.on('end',()=>{
    let payload;
    if(req.url==='/local/client/login'){res.end(JSON.stringify({userid:42,sess_key:'SYNTHETIC-SESSION'}));return;}
    if(req.url==='/Connect')payload={is_user:0};
    else if(req.url==='/UserCreate')payload={user:{userid:42}};
    else if(req.url==='/HomeShow')payload={user:{bp:homeReads++===0?20:5}};
    else if(req.url==='/TeamBattleSoloPartnerShow')payload={arthurs:[2,3,4].map(role=>({arthur_type:role,partners:[{userid:100+role,rental_idx:0}]}))};
    else if(req.url==='/TeamBattleSoloStart'){startReads++;payload=opening;}
    else if(req.url==='/TeamBattleSoloEnd')payload={is_clear:0};
    else{res.statusCode=404;res.end();return;}
    res.end(JSON.stringify({res_code:0})+'\n'+JSON.stringify(payload)+'\n{}');
  });
});
await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
const port=server.address().port;
const wrapper=path.join(output,'fixture.ps1');
fs.writeFileSync(wrapper,String.raw`
$global:order03ProcessReads=0
function Get-NetTCPConnection { [CmdletBinding()] param([int]$LocalPort,[string]$State) [pscustomobject]@{OwningProcess=42424} }
function Get-Process {
 [CmdletBinding()] param([int]$Id)
 $global:order03ProcessReads++
 $date=[DateTime]::Parse('2026-11-24T07:00:00Z').ToUniversalTime()
 if($global:order03ProcessReads -gt 1){$date=$date.AddMinutes(1)}
 [pscustomobject]@{StartTime=$date;MainModule=[pscustomobject]@{FileName=$env:ORDER03_EXE}}
}
function Start-Sleep { param([int]$Seconds) }
& $env:ORDER03_SCRIPT -TestRoot $env:ORDER03_OUTPUT -Port ([int]$env:ORDER03_PORT)
`);
let stdout='',stderr='';
try{
  const child=spawn('powershell.exe',['-NoProfile','-ExecutionPolicy','Bypass','-File',wrapper],{windowsHide:true,env:{...process.env,ORDER03_EXE:path.join(root,reproduction.build.executable),ORDER03_SCRIPT:script,ORDER03_OUTPUT:output,ORDER03_PORT:String(port),TEMP:output,TMP:output}});
  child.stdout.on('data',data=>stdout+=data);child.stderr.on('data',data=>stderr+=data);
  const code=await new Promise(resolve=>child.on('close',resolve));
  assert.equal(code,0,stdout+stderr);
  const directory=fs.readdirSync(output).find(name=>name.startsWith('Order03-Restart-')&&!name.endsWith('.zip'));
  const report=JSON.parse(fs.readFileSync(path.join(output,directory,'restart.json'),'utf8'));
  assert.equal(report.success,true);
  assert.equal(startReads,2);
  assert.equal(report.steps.find(step=>step.phase==='restore').sameOpeningPayload,true);
  assert.equal(report.steps.find(step=>step.phase==='abandon').bpDelta,15);
  assert.equal(JSON.stringify(report).includes('SYNTHETIC-SESSION'),false);
  assert.ok(fs.existsSync(path.join(output,directory+'.zip')));
  const validation={checkedAt:new Date().toISOString(),success:true,scriptSha256:crypto.createHash('sha256').update(fs.readFileSync(script)).digest('hex'),helpersSha256:crypto.createHash('sha256').update(fs.readFileSync(path.join(root,'scripts/Collect-Order03RuntimeSelection.ps1'))).digest('hex'),scope:'Local synthetic HTTP/process-change fixtures through the normal script entry point. Actual service restart remains untested.'};
  fs.writeFileSync(path.join(root,'evidence/ios-package-search/order03-restart-probe-tests.json'),JSON.stringify(validation,null,2)+'\n');
  console.log(JSON.stringify(validation,null,2));
}finally{server.close();}
