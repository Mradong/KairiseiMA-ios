import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const prefix='evidence/ios-package-search/';
const json=file=>JSON.parse(fs.readFileSync(path.join(root,file),'utf8'));
const sha=file=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,file))).digest('hex');
const result=json(prefix+'order03-real-battle-continue.json');
assert.equal(result.liveWorkflowCompleted,true);
const phase=name=>result.steps.find(step=>step.phase===name);
assert.equal(phase('engine_finished').victory,true);
assert.equal(phase('settlement').isClear,1);
assert.equal(phase('settlement').identicalRetry,true);
assert.equal(phase('paid_continue').debitedCrystals,50);
assert.equal(phase('paid_continue').identicalRetry,true);
assert.equal(phase('defeat_settlement').isClear,0);
for(const log of result.logs)assert.equal(sha(log.file),log.sha256);
const trace=name=>{
  const file=result.logs.find(log=>log.file.endsWith(name+'-steps.jsonl')).file;
  return fs.readFileSync(path.join(root,file),'utf8').trim().split('\n').map(line=>JSON.parse(line));
};
const victory=trace('victory'),defeat=trace('defeat');
assert.equal(victory.at(-1).state.EndType,1);
assert.equal(defeat.at(-1).state.EndType,2);
const revival=defeat.find(step=>step.operation===13);
assert(revival && revival.state.Members.filter(member=>member.MemberType<=4).some(member=>member.HP>0));
assert(defeat.at(-1).state.Members.filter(member=>member.MemberType<=4).every(member=>member.HP===0));
const report={
  checkedAt:new Date().toISOString(),success:true,
  realServiceVictoryAccepted:true,victoryRetryMatched:true,
  paidContinueAccepted:true,continueRetryMatched:true,continueDebit:50,
  localEngineRevivalObserved:true,realDefeatAccepted:true,
  actualServiceParametersAndDeliveredTables:true,
  traceRecords:{victory:victory.length,defeat:defeat.length},
  defeatFrozenResultRetry:'NOT_CAPTURED',
  actualServiceProcessRestartRecovery:'NOT_TESTED',
  sourceReport:{file:prefix+'order03-real-battle-continue.json',sha256:sha(prefix+'order03-real-battle-continue.json')},
  logs:result.logs,
  scriptSha256:sha('scripts/audit-order03-real-battle.mjs'),
  scope:'Real dedicated service identities, source/table-based local Go engine logs, accepted victory/paid continue/defeat requests. Actual original client/device gameplay, frozen defeat-result retry and live service restart recovery remain unverified.'
};
fs.writeFileSync(path.join(root,prefix+'order03-real-battle-validation.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report,null,2));
