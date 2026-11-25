import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const input='inputs/server-order03/Order03-Restart-20261125T025522528Z-lyd';
const read=file=>fs.readFileSync(path.join(root,file));
const sha=file=>crypto.createHash('sha256').update(read(file)).digest('hex');
const load=file=>JSON.parse(read(file).toString('utf8').replace(/^\uFEFF/,''));
assert.equal(sha(input+'.zip'),'59d0da537169f88f7bf37d19a9786f006c040c14c1743f007b3b90dd6b312eef');
assert.equal(sha(input+'/preflight-result-lyd.json'),'4f6dc782c024bef3cc4244482b0352830f6629a751db440ae3387f0acd71ab71');
const sums=/^([a-f0-9]{64})  restart-lyd\.json\s*$/.exec(read(input+'/SHA256SUMS-lyd.txt').toString('utf8'));
assert(sums);assert.equal(sums[1],sha(input+'/restart-lyd.json'));
const restart=load(input+'/restart-lyd.json'),preflight=load(input+'/preflight-result-lyd.json');
assert.equal(restart.schemaVersion,1);assert.equal(restart.collector,'order03-restart-20261125-1');
assert.equal(restart.success,false);assert.equal(restart.failureStage,'identity_creation');
assert.equal(restart.lastRequestRoute,'/local/client/login');assert.equal(restart.lastNetworkStatus,'Timeout');
assert.deepEqual(restart.steps,[]);assert(!restart.processBefore&&!restart.processAfter);
assert.equal(preflight.schema,1);assert.equal(preflight.state,'TEST_START_REQUESTED');
assert.equal(preflight.production_modified,false);assert.equal(preflight.test_service_start_requested,true);
const reproduction=load('evidence/ios-package-search/order03-reviewed-patch-reproduction.json');
const selection=load('evidence/ios-package-search/order03-runtime-selection-validation.json');
assert.equal(preflight.test_binary_sha256,reproduction.build.sha256);
assert.equal(preflight.resource_set_sha256,selection.selectedManifest.sha256);
assert.equal(preflight.content_id,'55c68a8971aafc9a137f79f16d5c2795bfb37c89b7f64bdca1542fc0eb1fd2bb');
assert(preflight.available_memory_mb>=preflight.minimum_available_mb);
const finished=Date.parse(restart.finishedUtc),requested=Date.parse(preflight.checked_utc);
assert(finished>Date.parse(restart.startedUtc)&&requested>finished);
const evidence='evidence/ios-package-search/order03-restart-result-20261125T025522528Z-validation-lyd.json';
const report={checkedAt:new Date().toISOString(),success:true,analysisCompleted:true,scriptSha256:sha('scripts/audit-order03-restart-result.mjs'),
  inputFiles:[input+'.zip',input+'/restart-lyd.json',input+'/SHA256SUMS-lyd.txt',input+'/preflight-result-lyd.json'].map(file=>({file,bytes:read(file).length,sha256:sha(file)})),checksumVerified:true,
  recoveryPassed:false,actualProcessRestartObserved:false,failureStage:restart.failureStage,lastRequestRoute:restart.lastRequestRoute,lastNetworkStatus:restart.lastNetworkStatus,
  preflight:{state:preflight.state,checkedUtc:preflight.checked_utc,secondsAfterProbeFinished:(requested-finished)/1000,productionModified:false,binaryAndManifestMatchReviewedInputs:true,availableMemoryMB:preflight.available_memory_mb,minimumAvailableMB:preflight.minimum_available_mb},
  sideEffects:{loginRequestedOnce:true,dedicatedAccountPersistence:'UNKNOWN after timed-out request',characterCreationRequested:false,battleEntryRequested:false,bpDebitRequested:false,rewardRequested:false},
  conclusion:'Login timed out before character creation and battle entry. Submitted preflight reports a service-start request after probe failure; it does not prove service readiness or the timeout cause. Battle restart recovery remains untested in this attempt.',
  scope:'Checks the received sanitized report and preflight identities/times. Does not infer server account creation or root cause from a timeout.'};
fs.writeFileSync(path.join(root,evidence),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify({evidence,analysisCompleted:true,recoveryPassed:false,failureStage:report.failureStage,networkStatus:report.lastNetworkStatus,preflightSecondsAfterFailure:report.preflight.secondsAfterProbeFinished},null,2));
