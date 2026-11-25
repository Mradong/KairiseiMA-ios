import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const input='inputs/server-order03/Order03-Restart-20261125T031514670Z-lyd';
const read=file=>fs.readFileSync(path.join(root,file));
const sha=file=>crypto.createHash('sha256').update(read(file)).digest('hex');
const load=file=>JSON.parse(read(file).toString('utf8').replace(/^\uFEFF/,''));
assert.equal(sha(input+'.zip'),'a76e368f7c1ffd57574e3b3740ce429e017ee060b97dd3c504b0bff5ace29605');
const sums=/^([a-f0-9]{64})  restart-lyd\.json\s*$/.exec(read(input+'/SHA256SUMS-lyd.txt').toString('utf8'));
assert(sums);assert.equal(sums[1],sha(input+'/restart-lyd.json'));
const result=load(input+'/restart-lyd.json'),boundary=load('evidence/ios-package-search/order03-solo-persistence-boundary-lyd.json');
assert.equal(result.collector,'order03-restart-20261125-2');assert.equal(result.success,false);
assert.equal(result.failureStage,'restore_start');assert.equal(result.failureClass,'MethodInvocationException');
assert.equal(result.serviceReadiness.startup.healthState,'PASS');assert.equal(result.serviceReadiness.afterRestart.healthState,'PASS');
assert(Date.parse(result.processAfter.startUtc)>Date.parse(result.processBefore.startUtc));
for(const process of [result.processBefore,result.processAfter]) {
  assert.equal(process.executable.sha256,'7704963e90c587aa4e8167fc7e74299f231e8b22f45a10012fbbe44301727918');
  assert.equal(process.executable.stable,true);
}
assert(result.steps.some(step=>step.phase==='entry'&&step.accepted&&step.initialBP===20));
assert(result.steps.some(step=>step.phase==='failure_abandon'&&step.accepted));
assert.equal(result.sideEffects.clearRewardRequested,false);
assert(boundary.success&&boundary.assertions.freshRepositoryOpeningAbsent&&boundary.assertions.paidBPBalanceSurvives);
const report={checkedAt:new Date().toISOString(),analysisCompleted:true,scriptSha256:sha('scripts/audit-order03-restart-latest.mjs'),
  inputFiles:[input+'.zip',input+'/restart-lyd.json',input+'/SHA256SUMS-lyd.txt'].map(file=>({file,sha256:sha(file),bytes:read(file).length})),checksumVerified:true,
  actualProcessRestartObserved:true,processStartBefore:result.processBefore.startUtc,processStartAfter:result.processAfter.startUtc,executableSha256:result.processAfter.executable.sha256,
  startupAndPostRestartReadinessPassed:true,reloginReachedIdentityCheckedRestoreStage:true,recoveryPassed:false,failureStage:result.failureStage,failureClass:result.failureClass,failureAbandonAccepted:true,
  diagnosticLimit:'Failure cleanup replaced lastRequestRoute/lastBusinessCode and cleared HTTP/network fields. The actual failed restore request HTTP status is NOT_CAPTURED; successful End cleanup is not the failed request diagnostic.',
  sourceBoundary:{evidence:'evidence/ios-package-search/order03-solo-persistence-boundary-lyd.json',sha256:sha('evidence/ios-package-search/order03-solo-persistence-boundary-lyd.json'),sameProcessOpeningRecovered:true,freshRepositoryOpeningAbsent:true,paidBalancePersists:true,localFreshEntryHTTP:400,observedSourceProcessRestartRecovery:'NOT_SUPPORTED by serving-lifetime solo cache'},
  sideEffects:{newDedicatedAccountAndCharacter:1,acceptedEntries:1,configuredEntryBP:15,clearRewardRequested:false,abandonAccepted:true,finalBP:'NOT_CAPTURED; no refund or second-charge claim'},
  conclusion:'The received run proves a real process restart and failure to restore Start after successful identity-checked relogin. Reviewed source and separate SQLite/HTTP fixtures explain the absent durable opening. The precise现场 HTTP error remains uncaptured.',
  scope:'Actual received server observation and explicitly separate local source/SQLite fixture evidence. No existing account/database/credential access.'};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/order03-restart-result-20261125T031514670Z-validation-lyd.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify({analysisCompleted:true,actualProcessRestartObserved:true,recoveryPassed:false,sourceRestartRecovery:'NOT_SUPPORTED',failedRestoreHTTP:'NOT_CAPTURED'},null,2));
