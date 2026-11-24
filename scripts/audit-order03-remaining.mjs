import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import { fileURLToPath } from 'node:url';
import { validateStaticProjections } from './order03-runtime-supplement-validation.mjs';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const input = 'inputs/server-order03/Order03-Remaining-20261124T074548237Z-2260ff';
const previousInput = 'inputs/server-order03/Order03-Runtime-Supplement-20261124T010145.658885200Z';
const evidence = 'evidence/ios-package-search/';
const bytes = filename => fs.readFileSync(path.join(root, filename));
const sha = filename => crypto.createHash('sha256').update(bytes(filename)).digest('hex');
const json = filename => JSON.parse(bytes(filename).toString('utf8').replace(/^\uFEFF/, ''));
assert.equal(sha(input + '.zip'), '17ca50a8d3aad9efa8224ead51b52d08e7e881e5acedf81beda4a9d77e5263ed');
const capture = json(input + '/capture.json');
const runtime = json(input + '/runtime.json');
const source = json(input + '/source.json');
const packageRecord = json(evidence + 'order03-remaining-collector-package.json');
const oldRuntime = json(previousInput + '/runtime-observation.json');
assert.equal(capture.collector, 'order03-remaining-20261124-1');
assert.equal(capture.scriptIdentity.sha256, packageRecord.files.find(f => f.name === 'Collect-Order03RemainingEvidence.ps1').sha256);
assert.equal(capture.sideEffects.modifiedInputFiles, 0);
assert.equal(capture.sideEffects.gameAccountOperations, 0);
assert.equal(capture.sideEffects.networkRequests, 0);
assert.equal(capture.sideEffects.serviceStartsStopsOrRestarts, 0);
const sums = bytes(input + '/SHA256SUMS.txt').toString('utf8').trim().split(/\r?\n/).map(line => {
  const match = /^([0-9a-f]{64})  (.+)$/.exec(line);
  assert(match, 'Malformed checksum line');
  const filename = match[2].replaceAll('\\', '/');
  assert(!filename.startsWith('/') && !filename.includes('..') && !filename.includes(':'));
  assert.equal(sha(input + '/' + filename), match[1]);
  return { file: input + '/' + filename, sha256: match[1], bytes: bytes(input + '/' + filename).length };
});
assert.equal(new Set(sums.map(entry => entry.file)).size, 9);
for (const record of capture.files) {
  assert.equal(record.state, 'copied_original_bytes');
  assert.equal(sha(input + '/' + record.role), record.identity.sha256);
  assert.equal(bytes(input + '/' + record.role).length, record.identity.bytes);
}
const processBefore = runtime.processBefore;
assert.deepEqual(processBefore, runtime.processAfter);
assert.equal(processBefore.state, 'observed');
assert.equal(processBefore.port, 27040);
assert.equal(processBefore.listeners.length, 1);
assert.equal(runtime.sameObservedProcess, true);
assert.equal(processBefore.listeners[0].imageIdentity.sha256, oldRuntime.on_disk_server_binary.identity.sha256);
assert.equal(runtime.executable.sha256, oldRuntime.on_disk_server_binary.identity.sha256);
assert.equal(runtime.selectedConfigIdentity.sha256, oldRuntime.config_identity_only.sha256);
assert.equal(runtime.selectedResourceManifestIdentity.sha256, oldRuntime.resource_manifest_identity.sha256);
const expected = json(evidence + 'order03-expected-tables.json');
assert.equal(runtime.tables.length, 29);
assert.equal(new Set(runtime.tables.map(t => t.name)).size, 29);
for (const table of runtime.tables) {
  const prior = expected.tables.find(t => t.name === table.name);
  assert(prior && table.matchesReceived && table.identity.stable);
  assert.equal(table.identity.sha256, prior.sha256);
  assert.equal(table.identity.bytes, prior.bytes);
}

// Reproduce the previously received field projection from the newly received
// original static JSON. Only omitted optional numeric reward contexts default
// to zero, matching their JSON/Go zero-value representation.
const rewardDefaults = { tower_id: 0, tower_floor: 0 };
function select(record, fields, defaults = {}) {
  const result = {};
  for (const field of fields) {
    if (Object.hasOwn(record, field)) result[field] = record[field];
    else if (Object.hasOwn(defaults, field)) {
      result[field] = 0;
      defaults[field]++;
    } else throw new Error('Missing required projection field ' + field);
  }
  return result;
}
const replayFields = ['boss_id','enemy_party_id','enemy_type','cost_initial','burst_gauge_initial','hold_max','end_turn'];
const rewardFields = ['boss_id','stage_quest_area_id','stage_quest_stage_id','tower_id','tower_floor'];
function project(raw, groupField) {
  const projected = {};
  projected[groupField] = raw[groupField].map(group => ({
    ...select(group, ['0','1']),
    '10': group['10'].map(quest => select(quest, ['0','1','5','7','24']))
  }));
  projected.replays = raw.replays.map(replay => {
    const value = select(replay, replayFields);
    if (Object.hasOwn(replay, 'battles')) value.battles = replay.battles.map(wave => select(wave, ['enemy_party_id','enemy_type']));
    return value;
  });
  projected.rewards = raw.rewards.map(reward => select(reward, rewardFields, rewardDefaults));
  if (groupField === 'groups') {
    projected.own_deck_variants = raw.own_deck_variants.map(v => select(v, ['source_boss_id','boss_id']));
    projected.deduplicated_families = raw.deduplicated_families.map(family => ({
      ...select(family, ['canonical_family_id','duplicate_family_id']),
      boss_aliases: family.boss_aliases.map(alias => select(alias, ['canonical_boss_id','duplicate_boss_id']))
    }));
  }
  return projected;
}
const rawBattle = json(input + '/static/battle.json');
const rawNormal = json(input + '/static/normal_quest.json');
assert.equal(sha(input + '/static/battle.json'), oldRuntime.static_resources.find(x => x.role === 'battle').source_identity.sha256);
assert.equal(sha(input + '/static/normal_quest.json'), oldRuntime.static_resources.find(x => x.role === 'normal_quest').source_identity.sha256);
const projectedBattle = project(rawBattle, 'groups');
const projectedNormal = project(rawNormal, 'entry_groups');
assert.deepEqual(projectedBattle, json(previousInput + '/battle-static-projection.json'));
assert.deepEqual(projectedNormal, json(previousInput + '/normal_quest-static-projection.json'));
const statics = validateStaticProjections(projectedBattle, projectedNormal,
  json('inputs/server-order03/Order03-Combat-Verified-20261123/runtime-static-replays.json'));
assert.equal(source.state, 'not_found');
assert.equal(source.files.length, 0);
assert.equal(json(input + '/startup-observations.json').length, 0);
assert.equal(json(input + '/provided-evidence.json').length, 0);
const startupPath = input + '/deployment-scripts/Start-IOS-Test.ps1';
assert.equal(sha(startupPath), oldRuntime.startup_script_identity.identity.sha256);
const startup = bytes(startupPath).toString('utf8');
assert(startup.includes("'-resource-set',$resourceSet,'-platform-content',$profilePath"));
assert(startup.includes('[string]$config.resource_set'));
const report = {
  checkedAt: new Date().toISOString(), success: true, planItem: '03',
  archive: { file: input + '.zip', sha256: sha(input + '.zip'), bytes: bytes(input + '.zip').length, entries: 10, checksumEntriesVerified: sums.length },
  collectorIdentityMatchesDeliveredPackage: true,
  observation: {
    tablesMatched: runtime.tables.length,
    receivedDiskIdentitiesMatchEarlier: true,
    observedProcessStartUtc: processBefore.listeners[0].startUtc,
    earlierObservedProcessStartUtc: oldRuntime.process_before.matching_processes[0].start_utc,
    processStartChangedSinceEarlierObservation: processBefore.listeners[0].startUtc !== oldRuntime.process_before.matching_processes[0].start_utc,
    sameObservedProcessDuringCapture: true,
    goVersion: runtime.goBuildInfo.goVersion,
    embeddedModules: runtime.goBuildInfo.modules,
    buildSettings: runtime.goBuildInfo.settings,
    effectiveLoadedContent: 'UNKNOWN', deployedSourceCommit: 'UNKNOWN'
  },
  rawStaticProjection: {
    reproduced: true, originalStaticHashesMatchEarlierClaims: true,
    battleRows: rawBattle.replays.length, normalQuestRows: rawNormal.replays.length,
    battleProjectedFields: Object.keys(projectedBattle), normalProjectedFields: Object.keys(projectedNormal),
    omittedOptionalRewardFieldsDefaultedToZero: rewardDefaults,
    stats: statics.stats,
    scope: 'Newly received original static JSON reproduces every previously retained projection field. Extra raw fields including seed, rewards, recommendations, schedule and tower profiles were not executed or fully validated. CSV export provenance and actual process loading remain separate unknowns.'
  },
  startupScript: { file: startupPath, sha256: sha(startupPath), resourceSetAndPortSelectionReviewedStatically: true, actualProcessInvocationVerified: false },
  missing: { sourceFiles: true, startupLogs: true, providedContractEvidence: true, csvOriginalVersionAndConversion: true, successfulContinueAndRealSettlement: true, processRestartRecovery: true },
  inputFiles: sums,
  auditSourceSha256: sha('scripts/audit-order03-remaining.mjs'),
  scope: 'Offline validation of user-supplied server capture. No received code execution, service request, account access, deployment or source-table writes. Order 03 remains incomplete.'
};
fs.writeFileSync(path.join(root, evidence + 'order03-remaining-validation.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({ success: report.success, archive: report.archive, observation: { ...report.observation, embeddedModules: report.observation.embeddedModules.length }, rawStaticProjection: report.rawStaticProjection, missing: report.missing }, null, 2));
