import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {validateFinalReceipts} from './order03-final-receipt-validation.mjs';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const executable='1c1488558e2ed859cb8a9f36b8720b125c0dbceb3ac3c83e3bec2603e1fdf2c1';
const probe='c1113db640c98006be76cbef8d92b5261f0d2149c70351186445353eaae04998';
const startup='9aef0e3c2ed167d476b4fc9b6135a3ae26229a574ba19ab8d82440ebd48ce6a6';
const identity={stable:true,sha256:executable,bytes:29750272};
const fixture={fixtureScope:'SYNTHETIC VALIDATOR TEST ONLY; NOT SERVER EVIDENCE',expectedExecutableSha256:executable,expectedExecutableBytes:29750272,expectedProbeSha256:probe,expectedStartupSha256:startup,
  install:{schemaVersion:1,collector:'order03-solo-fix-install-20261125-1',success:true,state:'installed',binaryAfter:identity,accountOrResourceFilesAccessed:false,serviceStartedByInstaller:false},
  pin:{schemaVersion:1,collector:'order03-startup-pin-20261125-1',success:true,update:{state:'pin_synchronized',binary:identity,expectedHashBefore:'7704963e90c587aa4e8167fc7e74299f231e8b22f45a10012fbbe44301727918',expectedHashAfter:executable,changedField:'expected_test_binary_sha256',startupScriptSha256:startup,otherConfigBytesChanged:false,startupScriptChanged:false,productionOrResourcesAccessed:false,configSha256Before:'a'.repeat(64),configSha256After:'b'.repeat(64),configurationChanged:true}},
  restart:{schemaVersion:1,collector:'order03-restart-20261125-3',success:true,stage:'complete',expectedExecutableSha256:executable,scriptIdentity:{stable:true,sha256:probe},processBefore:{executable:identity,startUtc:'2026-11-25T01:00:00Z'},processAfter:{executable:identity,startUtc:'2026-11-25T01:02:00Z'},startedUtc:'2026-11-25T01:01:00Z',finishedUtc:'2026-11-25T01:03:00Z',serviceReadiness:{startup:{healthState:'PASS'},afterRestart:{healthState:'PASS'}},steps:[{phase:'entry',accepted:true,initialBP:20,waveCount:1},{phase:'restore',sameIdentity:true,sameSeed:true,sameStartTime:true,sameOpeningPayload:true},{phase:'abandon',accepted:true,isClear:0,bpDelta:15,finalBP:5}],sideEffects:{expectedEntryBP:15,entryMaximum:1,clearRewardRequested:false,newDedicatedAccountsMaximum:1,newCharactersMaximum:1,restartPerformedByUser:true}}
};
assert.equal(validateFinalReceipts(structuredClone(fixture)).reportedSameOpeningVerified,true);
const lateStartup=structuredClone(fixture);lateStartup.restart.processBefore.startUtc='2026-11-25T01:01:30Z';
assert.equal(validateFinalReceipts(lateStartup).reportedProcessRestartVerified,true,'Initial readiness wait may precede service startup');
const rejected=[];
for(const [name,mutate] of [
  ['installation_only',data=>{data.restart.success=false;}],
  ['rollback_not_installation',data=>{data.install.state='rolled_back';}],
  ['wrong_program',data=>{data.restart.processAfter.executable.sha256='0'.repeat(64);}],
  ['unmatched_probe',data=>{data.restart.scriptIdentity.sha256='0'.repeat(64);}],
  ['unchanged_process',data=>{data.restart.processAfter.startUtc=data.restart.processBefore.startUtc;}],
  ['process_from_before_probe',data=>{data.restart.processAfter.startUtc='2026-11-25T01:00:30Z';}],
  ['changed_opening',data=>{data.restart.steps[1].sameOpeningPayload=false;}],
  ['duplicate_debit',data=>{data.restart.steps[2].bpDelta=30;data.restart.steps[2].finalBP=-10;}],
  ['clear_claim',data=>{data.restart.steps[2].isClear=1;}],
  ['unrelated_config_changed',data=>{data.pin.update.otherConfigBytesChanged=true;}],
  ['failure_in_success',data=>{data.restart.failureDiagnostic={route:'/TeamBattleSoloStart'};}]
]) {
  const data=structuredClone(fixture);mutate(data);assert.throws(()=>validateFinalReceipts(data),name);rejected.push({case:name,rejected:true});
}
const file='scripts/order03-final-receipt-validation.mjs';
const report={checkedAt:new Date().toISOString(),success:true,validatorSha256:crypto.createHash('sha256').update(fs.readFileSync(path.join(root,file))).digest('hex'),syntheticValidStructureAccepted:true,initialServiceStartsAfterReadinessWaitAccepted:true,rejected,scope:'Synthetic receipt structure/invariant tests only. No server execution, no current acceptance pass, no goal completion or fabricated real receipt.'};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/order03-final-receipt-validator-tests-lyd.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report,null,2));
