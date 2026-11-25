import assert from 'node:assert/strict';

const digest=value=>assert.match(value,/^[a-f0-9]{64}$/);
export function validateFinalReceipts({install,pin,restart,expectedExecutableSha256,expectedExecutableBytes,expectedProbeSha256,expectedStartupSha256}) {
  for(const value of [expectedExecutableSha256,expectedProbeSha256,expectedStartupSha256])digest(value);
  const identity=value=>{
    assert(value&&value.stable===true,'Stable identity required');
    assert.equal(value.sha256,expectedExecutableSha256,'Unexpected program version');
    assert.equal(value.bytes,expectedExecutableBytes,'Unexpected program size');
  };
  assert.equal(install.schemaVersion,1);assert.equal(install.collector,'order03-solo-fix-install-20261125-1');
  assert.equal(install.success,true,'Installation did not succeed');
  assert(['installed','already_installed'].includes(install.state),'Rollback is not installation');
  identity(install.binaryAfter);
  assert.equal(install.accountOrResourceFilesAccessed,false);
  assert.equal(install.serviceStartedByInstaller,false);
  assert.equal(pin.schemaVersion,1);assert.equal(pin.collector,'order03-startup-pin-20261125-1');
  assert.equal(pin.success,true,'Startup pin not synchronized');
  const sync=pin.update;assert(sync);
  assert(['pin_synchronized','already_synchronized'].includes(sync.state));identity(sync.binary);
  assert.equal(sync.expectedHashAfter,expectedExecutableSha256);
  assert(['7704963e90c587aa4e8167fc7e74299f231e8b22f45a10012fbbe44301727918',expectedExecutableSha256].includes(sync.expectedHashBefore),'Unreviewed original startup pin');
  assert.equal(sync.changedField,'expected_test_binary_sha256');
  assert.equal(sync.startupScriptSha256,expectedStartupSha256);
  assert.equal(sync.otherConfigBytesChanged,false);assert.equal(sync.startupScriptChanged,false);
  assert.equal(sync.productionOrResourcesAccessed,false);
  digest(sync.configSha256Before);digest(sync.configSha256After);
  assert.equal(sync.configurationChanged,sync.expectedHashBefore!==sync.expectedHashAfter);
  if(!sync.configurationChanged)assert.equal(sync.configSha256Before,sync.configSha256After);
  assert.equal(restart.schemaVersion,1);assert.equal(restart.collector,'order03-restart-20261125-3');
  assert.equal(restart.success,true,'Restart recovery did not succeed');assert.equal(restart.stage,'complete');
  assert.equal(restart.expectedExecutableSha256,expectedExecutableSha256);
  assert.equal(restart.scriptIdentity.sha256,expectedProbeSha256,'Probe source differs from delivered script');
  assert.equal(restart.scriptIdentity.stable,true);
  identity(restart.processBefore.executable);identity(restart.processAfter.executable);
  const before=Date.parse(restart.processBefore.startUtc),after=Date.parse(restart.processAfter.startUtc);
  const started=Date.parse(restart.startedUtc),finished=Date.parse(restart.finishedUtc);
  assert([before,after,started,finished].every(Number.isFinite));
  assert(before<after&&after>=started&&after<=finished,'No new process inside the probe interval');
  assert.equal(restart.serviceReadiness.startup.healthState,'PASS');
  assert.equal(restart.serviceReadiness.afterRestart.healthState,'PASS');
  const oneStep=name=>{const rows=restart.steps.filter(step=>step.phase===name);assert.equal(rows.length,1,'One '+name+' observation required');return rows[0];};
  const entry=oneStep('entry'),restore=oneStep('restore'),abandon=oneStep('abandon');
  assert.equal(entry.accepted,true);assert.equal(entry.initialBP,20);assert.equal(entry.waveCount,1);
  for(const field of ['sameIdentity','sameSeed','sameStartTime','sameOpeningPayload'])assert.equal(restore[field],true,'Restore failed: '+field);
  assert.equal(abandon.accepted,true);assert.equal(abandon.isClear,0);assert.equal(abandon.bpDelta,15);
  assert.equal(abandon.finalBP,entry.initialBP-15,'Single entry debit not established');
  assert.equal(restart.sideEffects.expectedEntryBP,15);assert.equal(restart.sideEffects.entryMaximum,1);
  assert.equal(restart.sideEffects.clearRewardRequested,false);
  assert.equal(restart.sideEffects.newDedicatedAccountsMaximum,1);
  assert.equal(restart.sideEffects.newCharactersMaximum,1);
  assert.equal(restart.sideEffects.restartPerformedByUser,true);
  assert(!restart.failureStage&&!restart.failureDiagnostic,'Failure diagnostics remain in success report');
  return {installationReceiptVerified:true,startupPinReceiptVerified:true,reportedProcessRestartVerified:true,reportedSameOpeningVerified:true,singleEntryBPDeltaVerified:true,nonClearAbandonVerified:true,
    scope:'Validation of user-supplied sanitized observations and delivered script identities. No direct client inspection of server memory, credentials, account database, device state or full gameplay.'};
}
