import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {validateRuntimeObservation, validateStaticProjections} from './order03-runtime-supplement-validation.mjs';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const prefix = 'evidence/ios-package-search/';
const read = p => fs.readFileSync(path.resolve(root, p));
const hash = b => crypto.createHash('sha256').update(b).digest('hex');
const json = p => JSON.parse(read(p).toString('utf8').replace(/^\uFEFF/, ''));
const imported = json(prefix + 'order03-runtime-supplement-import.json');
assert.equal(imported.archiveSha256, 'cf68822b1837e8f61845236c3c170eb535996a1e73cd419a233dc46b0bc3b964');
assert.equal(imported.allEntryBytesMatch, true); assert.equal(imported.entries.length, 3);
const inputFiles = imported.entries.map(e => {
  const relative = imported.isolatedCopy + '/' + e.name, bytes = read(relative);
  assert.equal(hash(bytes), e.sha256); assert.equal(bytes.length, e.bytes);
  return {file: relative, bytes: e.bytes, sha256: e.sha256};
});
const supplement = name => json(imported.isolatedCopy + '/' + name);
const oldBase = 'inputs/server-order03/Order03-Combat-Verified-20261123/';
const oldCollection = json(oldBase + 'collection-manifest.json');
const expected = json(prefix + 'order03-expected-tables.json');
const observation = supplement('runtime-observation.json');
const observationResult = validateRuntimeObservation(observation, expected, oldCollection);
for (const t of expected.tables) {
  const bytes = read(oldBase + 'tables/' + t.name);
  assert.equal(bytes.length, t.bytes); assert.equal(hash(bytes), t.sha256, 'Local original table changed');
}
const priorReplayBytes = read(oldBase + 'runtime-static-replays.json');
assert.equal(hash(priorReplayBytes), '259358a74bd68d0c877e93355bfc9575a1172437878092558ec9417847c3a431');
const battle = supplement('battle-static-projection.json'), normal = supplement('normal_quest-static-projection.json');
const statics = validateStaticProjections(battle, normal, JSON.parse(priorReplayBytes.toString('utf8')));

// The received JSON is a reduced static projection, not a complete raw runtime
// file. Compare it only with fields actually present in each artifact.
const defsPath = 'test-output/order03-independent-audit/projected-definitions.json';
const defsBytes = read(defsPath), defsHash = hash(defsBytes);
assert.equal(defsHash, '44f379a757c5be0e21f119ea314fc23b98ec0685b20964ef74cadcbcb2c8d113');
const defs = JSON.parse(defsBytes.toString('utf8'));
function references(replays) {
  const parties = new Set(), enemies = new Set(), skillIds = new Set(), aiIds = new Set(), roleGroups = new Set(), functions = new Set(), missing = [];
  let waves = 0, positiveEnemySlots = 0, skillReferences = 0, aiReferences = 0;
  for (const r of replays.values()) for (const wave of (r.battles || [{enemy_party_id: r.enemy_party_id, enemy_type: r.enemy_type}])) { waves++; parties.add(wave.enemy_party_id); }
  for (const id of parties) {
    const party = defs.EnemyParties[id]; if (!party) { missing.push({kind: 'enemy_party', id}); continue; }
    for (const slot of party.Slots) if (slot.EnemyID > 0) { positiveEnemySlots++; enemies.add(slot.EnemyID); }
  }
  for (const id of enemies) {
    if (!defs.Enemies[id]) missing.push({kind: 'enemy', id});
    const level = defs.EnemyLevels[id]; if (!level) { missing.push({kind: 'enemy_level', id}); continue; }
    const skills = [level.PassiveSkillID, ...(level.CallSkillIDs || []), ...(level.Actions || []).map(x => x.SkillID), ...(level.GaugeActions || []).map(x => x.SkillID)];
    for (const skill of skills) if (skill > 0) { skillReferences++; skillIds.add(skill); }
    for (const a of [...(level.Actions || []), ...(level.GaugeActions || [])]) if (a.AIConditionID > 0) { aiReferences++; aiIds.add(a.AIConditionID); }
  }
  for (const id of aiIds) if (!defs.EnemyAIOrders[id]) missing.push({kind: 'enemy_ai_order', id});
  for (const id of skillIds) {
    const variants = defs.Skills[id]; if (!variants?.length) { missing.push({kind: 'skill', id}); continue; }
    for (const s of variants) if (s.FunctionID > 0) roleGroups.add(s.FunctionID);
  }
  for (const id of roleGroups) {
    const roles = defs.SkillRoles[id]; if (!roles?.length) { missing.push({kind: 'role_group', id}); continue; }
    for (const role of roles) if (role.Function && role.Function !== 'NONE' && role.Function !== 'NULL') functions.add(role.Function);
  }
  for (const name of functions) if (!defs.RoleParamRules[name]) missing.push({kind: 'role_parameter_rule', name});
  return {replays: replays.size, waves, distinctParties: parties.size, positiveEnemySlotsInDistinctParties: positiveEnemySlots,
    distinctEnemyAndLevelIds: enemies.size, skillReferencesFromDistinctLevels: skillReferences, distinctSkills: skillIds.size,
    aiReferencesFromDistinctLevels: aiReferences, distinctAiIds: aiIds.size, distinctRoleGroups: roleGroups.size, distinctRoleFunctions: functions.size,
    missingReferences: missing, scope: 'Specified positive IDs in received replay waves, projected party enemy slots, common-key enemy/level, level passive/call/action/gauge skill IDs and action AI IDs, skill FunctionID groups and role-rule functions only. Counts are distinct definitions or field occurrences as labelled. No complete source JSON, all gameplay/parameters, gauges/reactions, presentation resources, account inputs, publication, or runtime execution claim.'};
}
const battleReferences = references(statics.battleReplays), normalReferences = references(statics.normalReplays);
assert.equal(battleReferences.missingReferences.length, 0); assert.equal(normalReferences.missingReferences.length, 0);
const priorQuest = json(prefix + 'order03-quest-variant-gap.json');
const prior = priorQuest.steps.find(x => x.path === '/TeamBattleSoloShow');
assert(priorQuest.success && prior);
const normalCategory = prior.categories.find(x => x.key === '9');
const countMatch = prior.entriesInStaticProjection === statics.battleEntries.size && prior.offsetMatchesWithOwnDeckRules === statics.variants.size &&
  prior.entriesAbsentFromStaticProjection === statics.variants.size + normalCategory.entries && prior.totalEntries === statics.battleEntries.size + statics.variants.size + normalCategory.entries;
assert(countMatch);
const candidateEntry = statics.battleEntries.get(30010102);
assert(candidateEntry); const candidateReplay = statics.battleReplays.get(30010102);
const fileHash = p => ({file: p, sha256: hash(read(p))});
const report = {
  checkedAt: new Date().toISOString(), planItem: '03', analysisCompleted: true, order03Complete: false,
  archive: {name: imported.archive, bytes: imported.archiveBytes, sha256: imported.archiveSha256, entriesVerified: inputFiles.length},
  collectedUtc: observation.collected_utc, collector: observation.collector,
  evidenceClass: 'Server-supplied typed observations and reduced static projections, independently compared offline with earlier received bytes. No client-side server-process access or network/business request.',
  receivedFiles: inputFiles,
  observation: {...observationResult, configSha256: observation.config_identity_only.sha256,
    serverDiskSha256: observation.on_disk_server_binary.identity.sha256, resourceManifestSha256: observation.resource_manifest_identity.sha256,
    stability: 'Server reports files stable on recheck; this package does not provide per-file recheck timestamps, process IDs or memory observations.',
    linkage: 'Selected disk executable matches the reported listener process image path before/after. This supports process/path/listener association as reported, not loaded file-byte/configuration identity or deployed source commit.'},
  staticSourceClaims: observation.static_resources.map(r => ({role: r.role, rawSourceBytes: r.source_identity.bytes, rawSourceSha256: r.source_identity.sha256, replayCount: r.raw_replay_count,
    projectionDerivation: 'UNKNOWN: raw runtime JSON and collector/projection source are not included; local hashes pin received projection bytes, not the claimed derivation.'})),
  statics: {...statics.stats, variantRuleCheck: '1043 distinct one-to-one sources; exact +100000000 offset; eligible source entry/reward fields and no replay/alias/normal ID collisions. Applying the reviewed local rule yields is_only_my_deck=1/start_rule=1.',
    sourceHash: fileHash('server-ios/server/internal/masterdata/battle_own_deck.go'),
    normalRewardContextCheck: '50 boss IDs each have two distinct contexts: standalone zeros and stage context matching area group/boss. The 100 rows are not treated as 100 boss IDs or duplicate reward grants.'},
  referenceChecks: {battle: battleReferences, normalQuest: normalReferences},
  historicalQuestComparison: {evidence: fileHash(prefix + 'order03-quest-variant-gap.json'),
    observedOnUtc: priorQuest.checkedAt, aggregateCountsAgree: countMatch,
    arithmetic: {battleEntryIds: statics.battleEntries.size, ownDeckVariants: statics.variants.size, historicalNormalCategoryEntries: normalCategory.entries, total: prior.totalEntries},
    historicalVariantMembership: 'UNKNOWN: prior redacted evidence records aggregate counts and shape checks, not the full returned boss-ID set; exact 1043-ID historical set equality cannot be reconstructed.',
    historicalNormalBossIdentity: 'UNKNOWN: earlier evidence saved one category-9 entry count, not its boss ID. New normal projection supplies 50 possible static entries; none is asserted to be that historical entry.',
    admissionAndPublication: 'UNKNOWN: received projection omits mutable publication and account context; static shape/count agreement is not live availability.'},
  candidate30010102: {entryPresent: true, onlyMyDeck: candidateEntry['1'], bpCost: candidateEntry['5'], continueFlag: candidateEntry['7'], startRule: candidateEntry['24'],
    waves: candidateReplay.battles || [{enemy_party_id: candidateReplay.enemy_party_id, enemy_type: candidateReplay.enemy_type}], allowedSafeAdmission: 'UNKNOWN', scope: 'Static fields only; continue flag does not prove recovery, charging or implemented continuation behavior.'},
  unknowns: {effectiveLoadedBytes: 'UNKNOWN', actualLoadedConfiguration: 'UNKNOWN', deployedSourceCommit: 'UNKNOWN', originalClientRegionVersionAndConversion: 'UNKNOWN',
    projectionDerivationFromClaimedRawSource: 'UNKNOWN', historicalNormalBossIdentity: 'UNKNOWN', livePublicationAndAdmission: 'UNKNOWN',
    safeStartRecoveryContinueSettlementContract: 'UNKNOWN: no versioned battle request/response/error evidence in this three-file package; previous source-derived limits remain scoped as historical reference.'},
  safety: {clientBusinessRequests: 0, serverChanges: false, realAccountsAccessed: false, productSnapshotCreatedOrPublished: false, sourceTablesModified: false},
  sourceHashes: [fileHash(prefix + 'order03-runtime-supplement-import.json'), fileHash(prefix + 'order03-expected-tables.json'), fileHash(oldBase + 'collection-manifest.json'),
    fileHash(oldBase + 'runtime-static-replays.json'), {file: defsPath, sha256: defsHash}, fileHash('scripts/order03-runtime-supplement-validation.mjs'), fileHash('scripts/audit-order03-runtime-supplement.mjs')],
  scope: 'This closes explicit static mapping and normal-quest projection gaps within the received fields. It strengthens reported server disk/process/listener observations and reaffirms unchanged delivered tables. It does not establish in-memory loading, actual source deployment, complete runtime-source projection, source-client version/transformation, exact historical list membership, publication, safe entry/recovery/settlement, real gameplay, Unity/ARM64 or device behavior. UNKNOWN values remain literal; candidate stays isolated.'
};
fs.writeFileSync(path.join(root, prefix + 'order03-runtime-supplement-validation.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({analysisCompleted: true, tables: observationResult.tablesCompared, staticCounts: statics.stats, battleReferences, normalReferences, aggregateHistoricalCountsAgree: countMatch, order03Complete: false, unknowns: report.unknowns}, null, 2));
