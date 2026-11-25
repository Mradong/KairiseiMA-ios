import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {spawn} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const script=process.env.ORDER03_TEST_SCRIPT||path.join(root,'scripts/Verify-Order03Restart.ps1');
const reproduction=JSON.parse(fs.readFileSync(path.join(root,'evidence/ios-package-search/order03-reviewed-patch-reproduction.json')));
const executable=process.env.ORDER03_TEST_EXE||path.join(root,reproduction.build.executable);
const expectedExecutableSha256=crypto.createHash('sha256').update(fs.readFileSync(executable)).digest('hex');
const opening={seed:123,start_time:456,battles:[{enemy_partyid:30010102,enemy_type:1}],cost_initial:3,hold_max:6,partner_deck:[]};
const cases=[];
for(const scenario of ['delayed_startup_and_recovery','service_unready','wrong_executable','restore_rejection_cleanup']) {
  const output=fs.mkdtempSync(path.join(root,'test-output/order03-restart-probe-'));
  let homeReads=0,startReads=0,loginReads=0,healthReads=0,healthPassed=false;
  const server=http.createServer((req,res)=>{
    req.resume();req.on('end',()=>{
      let payload;
      if(req.url==='/healthz') {
        assert.equal(req.method,'GET');healthReads++;
        if(scenario==='service_unready'||healthReads===1){res.statusCode=503;res.end('synthetic service starting');return;}
        healthPassed=true;res.end(JSON.stringify({state:'PASS'}));return;
      }
      assert.equal(healthPassed,true,'Persistent request before readiness');
      if(req.url==='/local/client/login'){
        loginReads++;
        if(loginReads===2){res.statusCode=503;res.end('fixture startup transient');return;}
        res.end(JSON.stringify({userid:42,sess_key:'SYNTHETIC-SESSION'}));return;
      }
      if(req.url==='/Connect')payload={is_user:0};
      else if(req.url==='/UserCreate')payload={user:{userid:42}};
      else if(req.url==='/HomeShow')payload={user:{bp:homeReads++===0?20:5}};
      else if(req.url==='/TeamBattleSoloPartnerShow')payload={arthurs:[2,3,4].map(role=>({arthur_type:role,partners:[{userid:100+role,rental_idx:0}]}))};
      else if(req.url==='/TeamBattleSoloStart'){
        startReads++;
        if(scenario==='restore_rejection_cleanup'&&startReads===2){res.statusCode=400;res.end('synthetic unavailable opening');return;}
        payload=opening;
      }
      else if(req.url==='/TeamBattleSoloEnd')payload={is_clear:0};
      else{res.statusCode=404;res.end();return;}
      res.end(JSON.stringify({res_code:0})+'\n'+JSON.stringify(payload)+'\n{}');
    });
  });
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  const wrapper=path.join(output,'fixture.ps1');
  fs.writeFileSync(wrapper,[
    '$global:order03ListenerReads=0',
    '$global:order03RestartRequested=$false',
    'function Get-NetTCPConnection {',
    ' [CmdletBinding()] param([int]$LocalPort,[string]$State)',
    ' $global:order03ListenerReads++',
    " if($env:ORDER03_SCENARIO -eq 'delayed_startup_and_recovery' -and $global:order03ListenerReads -eq 1){return}",
    ' [pscustomobject]@{OwningProcess=42424}',
    '}',
    'function Get-Process {',
    ' [CmdletBinding()] param([int]$Id)',
    " $date=[DateTime]::Parse('2026-11-24T07:00:00Z').ToUniversalTime()",
    ' if($global:order03RestartRequested){$date=$date.AddMinutes(1)}',
    ' [pscustomobject]@{StartTime=$date;MainModule=[pscustomobject]@{FileName=$env:ORDER03_EXE}}',
    '}',
    'function Write-Host {',
    ' param([string]$Object)',
    " if($Object -like 'Now restart ONLY*'){$global:order03RestartRequested=$true}",
    ' [Console]::WriteLine($Object)',
    '}',
    'function Start-Sleep { param([int]$Seconds) }',
    '& $env:ORDER03_SCRIPT -TestRoot $env:ORDER03_OUTPUT -Port ([int]$env:ORDER03_PORT) -ReadyAttempts 4 -ExpectedExecutableSha256 $env:ORDER03_EXPECTED_EXE_SHA'
  ].join('\n'));
  let stdout='',stderr='';
  try {
    const child=spawn('powershell.exe',['-NoProfile','-ExecutionPolicy','Bypass','-File',wrapper],{windowsHide:true,env:{...process.env,ORDER03_SCENARIO:scenario,ORDER03_EXE:scenario==='wrong_executable'?script:executable,ORDER03_EXPECTED_EXE_SHA:expectedExecutableSha256,ORDER03_SCRIPT:script,ORDER03_OUTPUT:output,ORDER03_PORT:String(server.address().port),TEMP:output,TMP:output}});
    child.stdout.on('data',data=>stdout+=data);child.stderr.on('data',data=>stderr+=data);
    const timer=setTimeout(()=>child.kill(),60000);
    const code=await new Promise(resolve=>child.on('close',resolve));clearTimeout(timer);
    assert.equal(code,0,stdout+stderr);
    const directory=fs.readdirSync(output).find(name=>name.startsWith('Order03-Restart-')&&!name.endsWith('.zip'));
    const report=JSON.parse(fs.readFileSync(path.join(output,directory,'restart-lyd.json'),'utf8'));
    assert.equal(report.collector,'order03-restart-20261125-3');
    assert.equal(report.scriptIdentity.stable,true);
    assert.equal(JSON.stringify(report).includes('SYNTHETIC-SESSION'),false);
    assert.ok(fs.existsSync(path.join(output,directory+'.zip')));
    if(scenario==='delayed_startup_and_recovery') {
      assert.equal(report.success,true,stdout+stderr);
      assert.equal(healthReads,3);assert.equal(startReads,2);assert.equal(loginReads,3);
      assert.equal(report.serviceReadiness.startup.attempts,3);
      assert.equal(report.serviceReadiness.afterRestart.healthState,'PASS');
      assert.equal(report.readinessFailureSamples[0].httpStatus,503);
      assert.equal(report.recoveryRequestRetries,1);
      assert.equal(report.recoveryFailureSamples[0].httpStatus,503);
      assert.equal(report.steps.find(step=>step.phase==='restore').sameOpeningPayload,true);
      assert.equal(report.steps.find(step=>step.phase==='abandon').bpDelta,15);
    } else if(scenario==='restore_rejection_cleanup') {
      assert.equal(report.success,false);assert.equal(report.failureStage,'restore_start');
      assert.equal(report.failureDiagnostic.route,'/TeamBattleSoloStart');assert.equal(report.failureDiagnostic.httpStatus,400);
      assert.equal(report.lastRequestRoute,'/TeamBattleSoloEnd');assert.equal(report.lastBusinessCode,0);
      assert.equal(report.steps.find(step=>step.phase==='failure_abandon').accepted,true);
    } else {
      assert.equal(report.success,false);assert.equal(report.failureStage,'service_readiness');
      assert.equal(startReads,0);assert.equal(loginReads,0);assert.equal(report.steps.length,0);
      assert.equal(healthReads,scenario==='service_unready'?4:0);
    }
    cases.push({scenario,passed:true,healthRequests:healthReads,loginRequests:loginReads,startRequests:startReads});
  } finally {await new Promise(resolve=>server.close(resolve));}
}
const validation={checkedAt:new Date().toISOString(),success:true,cases,expectedExecutableSha256,scriptSha256:crypto.createHash('sha256').update(fs.readFileSync(script)).digest('hex'),helpersSha256:crypto.createHash('sha256').update(fs.readFileSync(path.join(root,'scripts/Collect-Order03RuntimeSelection.ps1'))).digest('hex'),scope:'Local synthetic HTTP/process fixtures: startup/recovery HTTP 503, unavailable/wrong executable, and failed-restore diagnostics preserved through successful cleanup. No persistent request before readiness. Actual service restart remains untested.'};
const evidence=process.env.ORDER03_TEST_SCRIPT?'order03-restart-probe-packaged-tests-lyd.json':'order03-restart-probe-tests-lyd.json';
fs.writeFileSync(path.join(root,'evidence/ios-package-search',evidence),JSON.stringify(validation,null,2)+'\n');
console.log(JSON.stringify(validation,null,2));
