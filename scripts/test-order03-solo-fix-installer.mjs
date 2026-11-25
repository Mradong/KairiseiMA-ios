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
  'if($env:ORDER03_ROLLBACK -eq "1"){& $env:ORDER03_INSTALLER -TestRoot $env:ORDER03_FIXTURE_ROOT -ExpectedPayloadSha256 $env:ORDER03_PAYLOAD_SHA -Rollback}',
  'else{& $env:ORDER03_INSTALLER -TestRoot $env:ORDER03_FIXTURE_ROOT -ExpectedPayloadSha256 $env:ORDER03_PAYLOAD_SHA}',
  'if($LASTEXITCODE){exit $LASTEXITCODE}'
].join('\n'));
const cases=[];
try {
  for(const scenario of ['install_and_rollback','running_service','wrong_original']) {
    const testRoot=path.join(directory,scenario);fs.mkdirSync(path.join(testRoot,'bin'),{recursive:true});
    const live=path.join(testRoot,'bin/kairi-server.exe');
    if(scenario==='wrong_original')fs.writeFileSync(live,'SYNTHETIC-UNREVIEWED-BINARY');
    else fs.copyFileSync(path.join(root,original.build.executable),live);
    const untouched=path.join(testRoot,'untouched-marker-lyd.txt');fs.writeFileSync(untouched,'synthetic untouched marker');const marker=sha(untouched);
    function run(rollback=false) {
      const result=spawnSync('powershell.exe',['-NoProfile','-ExecutionPolicy','Bypass','-File',wrapper],{encoding:'utf8',windowsHide:true,timeout:30000,env:{...process.env,ORDER03_INSTALLER:script,ORDER03_FIXTURE_ROOT:testRoot,ORDER03_PAYLOAD_SHA:build.build.sha256,ORDER03_LISTENING:scenario==='running_service'?'1':'0',ORDER03_ROLLBACK:rollback?'1':'0',TEMP:directory,TMP:directory}});
      const receipts=fs.readdirSync(testRoot).filter(name=>name.startsWith('Order03-Solo-Fix-Install-')).sort();assert(receipts.length);
      const report=JSON.parse(fs.readFileSync(path.join(testRoot,receipts.at(-1))));return {result,report};
    }
    const before=sha(live);const first=run();
    if(scenario==='install_and_rollback') {
      assert.equal(first.result.status,0,first.result.stdout+first.result.stderr);assert.equal(first.report.state,'installed');
      assert.equal(sha(live),build.build.sha256);assert.equal(sha(path.join(testRoot,'bin',first.report.backupFile)),original.build.sha256);
      assert.equal(run().report.state,'already_installed');
      const rollback=run(true);assert.equal(rollback.result.status,0);assert.equal(rollback.report.state,'rolled_back');assert.equal(sha(live),before);
    } else {
      assert.equal(first.result.status,1);assert.equal(first.report.success,false);assert.equal(sha(live),before);
      assert.equal(fs.readdirSync(path.join(testRoot,'bin')).length,1);
    }
    assert.equal(sha(untouched),marker);assert.equal(first.report.accountOrResourceFilesAccessed,false);assert.equal(first.report.serviceStartedByInstaller,false);
    cases.push({scenario,passed:true,unrelatedMarkerUnchanged:true});
  }
  const report={checkedAt:new Date().toISOString(),success:true,scriptSha256:sha(script),cases,buildReceiptSha256:sha(path.join(root,'evidence/ios-package-search/order03-durable-solo-build-lyd.json')),scope:'Local file fixtures, ordinary PowerShell entry and mocked test-listener query. Install, already-installed, backup/rollback, running-service refusal and wrong-binary refusal verified. No actual service/account/production files accessed.'};
  const evidence=process.env.ORDER03_INSTALLER_SCRIPT?'order03-solo-fix-installer-packaged-tests-lyd.json':'order03-solo-fix-installer-tests-lyd.json';
  fs.writeFileSync(path.join(root,'evidence/ios-package-search',evidence),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report,null,2));
} finally {
  if(!process.env.ORDER03_INSTALLER_SCRIPT)fs.unlinkSync(payload);
}
