import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const build=JSON.parse(fs.readFileSync(path.join(root,'evidence/ios-package-search/order03-durable-solo-build-lyd.json')));
const original=JSON.parse(fs.readFileSync(path.join(root,'evidence/ios-package-search/order03-reviewed-patch-reproduction.json')));
const directory=fs.mkdtempSync(path.join(root,'test-output/order03-solo-installer-lyd-'));
const script=process.env.ORDER03_INSTALLER_SCRIPT||path.join(root,'scripts/Install-Order03SoloFix.ps1');
const scriptRoot=path.dirname(script);
const sha=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const payload=path.join(scriptRoot,'kairi-server-durable-solo-lyd.exe');
if(!process.env.ORDER03_INSTALLER_SCRIPT)fs.copyFileSync(path.join(root,build.build.executable),payload);
assert.equal(sha(payload),build.build.sha256);
const wrapper=path.join(directory,'fixture-lyd.ps1');
fs.writeFileSync(wrapper,[
  'function Get-NetTCPConnection { [CmdletBinding()] param([int[]]$LocalPort,[string]$State) if($env:ORDER03_LISTENING -eq "1"){[pscustomobject]@{LocalPort=27040}} }',
  '$guard=$null',
  'if($env:ORDER03_LOCK_CONFIG -eq "1"){$guard=[IO.File]::Open((Join-Path $env:ORDER03_FIXTURE_ROOT "isolated-config.json"),[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)}',
  'try {',
  'if($env:ORDER03_ROLLBACK -eq "1"){& $env:ORDER03_INSTALLER -TestRoot $env:ORDER03_FIXTURE_ROOT -ExpectedPayloadSha256 $env:ORDER03_PAYLOAD_SHA -Rollback}',
  'else{& $env:ORDER03_INSTALLER -TestRoot $env:ORDER03_FIXTURE_ROOT -ExpectedPayloadSha256 $env:ORDER03_PAYLOAD_SHA}',
  '} finally {if($guard){$guard.Dispose()}}',
  'if($LASTEXITCODE){exit $LASTEXITCODE}'
].join('\n'));
const cases=[];
try {
  for(const scenario of ['install_and_rollback','running_service','wrong_original','locked_configuration']) {
    const testRoot=path.join(directory,scenario);fs.mkdirSync(path.join(testRoot,'bin'),{recursive:true});
    const live=path.join(testRoot,'bin/kairi-server.exe');
    if(scenario==='wrong_original')fs.writeFileSync(live,'SYNTHETIC-UNREVIEWED-BINARY');
    else fs.copyFileSync(path.join(root,original.build.executable),live);
    fs.copyFileSync(path.join(root,'inputs/server-order03/Order03-Remaining-20261124T074548237Z-2260ff/deployment-scripts/Start-IOS-Test.ps1'),path.join(testRoot,'Start-IOS-Test.ps1'));
    fs.writeFileSync(path.join(testRoot,'isolated-config.json'),JSON.stringify({schema:1,production_root:path.join(directory,'production-sibling'),http_port:27040,battle_port:27041,admin_port:0,expected_test_binary_sha256:original.build.sha256},null,2)+'\n');
    const untouched=path.join(testRoot,'untouched-marker-lyd.txt');fs.writeFileSync(untouched,'synthetic untouched marker');const marker=sha(untouched);
    function run(rollback=false) {
      const result=spawnSync('powershell.exe',['-NoProfile','-ExecutionPolicy','Bypass','-File',wrapper],{encoding:'utf8',windowsHide:true,timeout:30000,env:{...process.env,ORDER03_INSTALLER:script,ORDER03_FIXTURE_ROOT:testRoot,ORDER03_PAYLOAD_SHA:build.build.sha256,ORDER03_LISTENING:scenario==='running_service'?'1':'0',ORDER03_LOCK_CONFIG:scenario==='locked_configuration'?'1':'0',ORDER03_ROLLBACK:rollback?'1':'0',TEMP:directory,TMP:directory}});
      const receipts=fs.readdirSync(testRoot).filter(name=>name.startsWith('Order03-Solo-Fix-Install-')).sort();assert(receipts.length);
      const report=JSON.parse(fs.readFileSync(path.join(testRoot,receipts.at(-1))));return {result,report};
    }
    const before=sha(live);const first=run();
    if(scenario==='install_and_rollback') {
      assert.equal(first.result.status,0,first.result.stdout+first.result.stderr);assert.equal(first.report.state,'installed');
      assert.equal(sha(live),build.build.sha256);assert.equal(sha(path.join(testRoot,'bin',first.report.backupFile)),original.build.sha256);
      assert.equal(JSON.parse(fs.readFileSync(path.join(testRoot,'isolated-config.json'))).expected_test_binary_sha256,build.build.sha256);
      assert.equal(run().report.state,'already_installed');
      const rollback=run(true);assert.equal(rollback.result.status,0);assert.equal(rollback.report.state,'rolled_back');assert.equal(sha(live),before);
      assert.equal(JSON.parse(fs.readFileSync(path.join(testRoot,'isolated-config.json'))).expected_test_binary_sha256,original.build.sha256);
    } else {
      assert.equal(first.result.status,1);assert.equal(first.report.success,false);assert.equal(sha(live),before);
      if(scenario==='locked_configuration') {
        assert.equal(first.report.programRecoveredToBefore,true);
        assert.equal(JSON.parse(fs.readFileSync(path.join(testRoot,'isolated-config.json'))).expected_test_binary_sha256,original.build.sha256);
      } else assert.equal(fs.readdirSync(path.join(testRoot,'bin')).length,1);
    }
    assert.equal(sha(untouched),marker);assert.equal(first.report.accountOrResourceFilesAccessed,false);assert.equal(first.report.serviceStartedByInstaller,false);
    cases.push({scenario,passed:true,unrelatedMarkerUnchanged:true});
  }
  const report={checkedAt:new Date().toISOString(),success:true,scriptSha256:sha(script),cases,buildReceiptSha256:sha(path.join(root,'evidence/ios-package-search/order03-durable-solo-build-lyd.json')),scope:'Local binary/config fixtures: install and rollback synchronize the reviewed startup pin; already-installed is idempotent; running/wrong-binary refusals and locked-config failure restore the prior program and pin. No actual service/account/production files accessed.'};
  const evidence=process.env.ORDER03_INSTALLER_SCRIPT?'order03-solo-fix-installer-packaged-tests-lyd.json':'order03-solo-fix-installer-tests-lyd.json';
  fs.writeFileSync(path.join(root,'evidence/ios-package-search',evidence),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report,null,2));
} finally {
  if(!process.env.ORDER03_INSTALLER_SCRIPT)fs.unlinkSync(payload);
}
