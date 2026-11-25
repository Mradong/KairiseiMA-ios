import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const load=file=>JSON.parse(fs.readFileSync(path.join(root,file),'utf8'));
const build=load('evidence/ios-package-search/order03-durable-solo-build-lyd.json');
const original=load('evidence/ios-package-search/order03-reviewed-patch-reproduction.json');
const script=process.env.ORDER03_PIN_SCRIPT||path.join(root,'scripts/Sync-Order03StartupPin.ps1');
const startup='inputs/server-order03/Order03-Remaining-20261124T074548237Z-2260ff/deployment-scripts/Start-IOS-Test.ps1';
const output=fs.mkdtempSync(path.join(root,'test-output/order03-startup-pin-lyd-'));
const sha=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const cases=[];
for(const scenario of ['new_program_pin_and_original_preflight','wrong_program','changed_checker','unreviewed_pin','restore_original_pin']) {
  const directory=path.join(output,scenario),testRoot=path.join(directory,'test'),production=path.join(directory,'production');
  fs.mkdirSync(path.join(testRoot,'bin'),{recursive:true});fs.mkdirSync(production);
  fs.copyFileSync(path.join(root,original.build.executable),path.join(production,'kairi-server.exe'));
  const binary=path.join(testRoot,'bin/kairi-server.exe');
  if(scenario==='wrong_program')fs.writeFileSync(binary,'SYNTHETIC-UNREVIEWED');
  else fs.copyFileSync(path.join(root,scenario==='restore_original_pin'?original.build.executable:build.build.executable),binary);
  fs.copyFileSync(path.join(root,startup),path.join(testRoot,'Start-IOS-Test.ps1'));
  if(scenario==='changed_checker')fs.appendFileSync(path.join(testRoot,'Start-IOS-Test.ps1'),'\n# SYNTHETIC ALTERATION\n');
  fs.writeFileSync(path.join(production,'resource-set.json'),'{}');
  const config={schema:1,production_root:production,http_port:27040,battle_port:27041,admin_port:0,expected_test_binary_sha256:scenario==='unreviewed_pin'?'a'.repeat(64):scenario==='restore_original_pin'?build.build.sha256:original.build.sha256,expected_production_binary_sha256:original.build.sha256,resource_set:path.join(production,'resource-set.json'),expected_resource_set_sha256:sha(path.join(production,'resource-set.json')),ios_profile:'ios/missing-profile.json',expected_ios_profile_sha256:'b'.repeat(64),expected_ios_content_id:'synthetic-fixture',phone_advertise_host:'127.0.0.1',unrelated_original_digest:original.build.sha256,note:'中文测试，保留原文'};
  const configFile=path.join(testRoot,'isolated-config.json');
  const beforeText='\ufeff'+JSON.stringify(config,null,4).replaceAll('\n','\r\n')+'\r\n';fs.writeFileSync(configFile,beforeText);const beforeHash=sha(configFile);
  function runPin() {
    const result=spawnSync('powershell.exe',['-NoProfile','-ExecutionPolicy','Bypass','-File',script,'-TestRoot',testRoot],{encoding:'utf8',windowsHide:true,timeout:30000,env:{...process.env,TEMP:output,TMP:output}});
    const receipts=fs.readdirSync(testRoot).filter(file=>file.startsWith('Order03-Startup-Pin-')).sort();assert(receipts.length);
    return {result,report:JSON.parse(fs.readFileSync(path.join(testRoot,receipts.at(-1))))};
  }
  function runOriginalPreflight() {
    return spawnSync('powershell.exe',['-NoProfile','-ExecutionPolicy','Bypass','-File',path.join(testRoot,'Start-IOS-Test.ps1'),'-Mode','Phone','-CheckOnly'],{encoding:'utf8',windowsHide:true,timeout:30000,env:{...process.env,TEMP:output,TMP:output}});
  }
  if(scenario==='new_program_pin_and_original_preflight') {
    const refused=runOriginalPreflight();assert.equal(refused.status,1);assert(refused.stdout.includes('File SHA-256 differs:'));
  }
  const result=runPin();
  if(['wrong_program','changed_checker','unreviewed_pin'].includes(scenario)) {
    assert.equal(result.result.status,1);assert.equal(result.report.success,false);assert.equal(sha(configFile),beforeHash);
  } else {
    assert.equal(result.result.status,0,result.result.stdout+result.result.stderr);assert.equal(result.report.success,true);
    const expected=scenario==='restore_original_pin'?original.build.sha256:build.build.sha256;
    const expectedText=beforeText.replace('"expected_test_binary_sha256": "'+config.expected_test_binary_sha256+'"','"expected_test_binary_sha256": "'+expected+'"');
    assert.equal(fs.readFileSync(configFile,'utf8'),expectedText,'Only the selected pin may change; BOM/CRLF/other values retained');
    const synchronized=sha(configFile);assert.equal(runPin().report.update.state,'already_synchronized');assert.equal(sha(configFile),synchronized);
    if(scenario==='new_program_pin_and_original_preflight') {
      const acceptedPin=runOriginalPreflight();assert.equal(acceptedPin.status,2,acceptedPin.stdout+acceptedPin.stderr);
      const preflight=JSON.parse(fs.readFileSync(path.join(testRoot,'preflight-result.json'),'utf8').replace(/^\uFEFF/,''));
      assert.equal(preflight.state,'WAITING_FOR_IOS_RESOURCES');assert.equal(preflight.test_binary_sha256,build.build.sha256);
      assert.equal(fs.existsSync(path.join(testRoot,'_local')),false);
      fs.mkdirSync(path.join(testRoot,'ios'));fs.writeFileSync(path.join(testRoot,'ios','missing-profile.json'),'{}');
      const refusedProfile=runOriginalPreflight();assert.equal(refusedProfile.status,1);assert(refusedProfile.stdout.includes('File SHA-256 differs:'));
    }
  }
  cases.push({scenario,passed:true});
}
const report={checkedAt:new Date().toISOString(),success:true,scriptSha256:sha(script),startupSource:{file:startup,sha256:sha(path.join(root,startup))},cases,scope:'Local config/file fixtures and received real Start-IOS-Test.ps1 -CheckOnly. Original startup rejects the old pin with the new binary, then passes that pin after sync and still refuses missing iOS resources. No actual service/database startup. BOM, CRLF, other pins, refusals and original-program rollback pin are checked.'};
fs.writeFileSync(path.join(root,'evidence/ios-package-search',process.env.ORDER03_PIN_SCRIPT?'order03-startup-pin-packaged-tests-lyd.json':'order03-startup-pin-tests-lyd.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report,null,2));
