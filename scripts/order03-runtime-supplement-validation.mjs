import assert from 'node:assert/strict';

const object = x => x !== null && typeof x === 'object' && !Array.isArray(x);
const keys = (x, expected, label) => {
  assert(object(x), label + ': expected object');
  assert.deepEqual(Object.keys(x).sort(), [...expected].sort(), label + ': unexpected fields');
};
const integer = (x, label, minimum = 0) => assert(Number.isSafeInteger(x) && x >= minimum, label + ': invalid integer');
const identity = (x, label) => {
  keys(x, ['bytes', 'sha256'], label); integer(x.bytes, label + '.bytes', 1);
  assert.match(x.sha256, /^[0-9a-f]{64}$/, label + '.sha256');
};
const utc = (s, label) => { assert.equal(typeof s, 'string'); assert.match(s, /^\d{4}-\d{2}-\d{2}T[^\s]+Z$/); assert(Number.isFinite(Date.parse(s)), label + ': invalid time'); };
const unique = (items, key, label) => {
  const result = new Map(); for (const x of items) { integer(x[key], label + '.' + key, 1); assert(!result.has(x[key]), label + ': duplicate ID'); result.set(x[key], x); } return result;
};
const same = (a, b) => assert.deepEqual(a, b);
const unknown = (x, label) => { assert.equal(typeof x, 'string'); assert(x.startsWith('UNKNOWN:'), label + ': unsupported certainty'); };

export function validateRuntimeObservation(o, expected, previous) {
  keys(o, ['business_requests','collected_utc','collector','config_identity_only','effective_loaded_bytes','matched_tables','observed_files_stable_on_recheck','on_disk_server_binary','privacy','process_after','process_before','quest_publication_and_admission','resource_manifest_identity','schema_version','selected_http_port','source_client_and_conversion','startup_script_identity','static_resources','tables','total_tables'], 'runtime observation');
  assert.equal(o.schema_version, 1); assert.equal(o.business_requests, 0); assert.equal(o.selected_http_port, 27040); utc(o.collected_utc, 'collected');
  assert.equal(o.collector, 'order03-runtime-observation-20261124-1'); assert.equal(typeof o.privacy, 'string');
  unknown(o.effective_loaded_bytes, 'effective bytes'); unknown(o.source_client_and_conversion, 'source client'); unknown(o.quest_publication_and_admission, 'admission');
  identity(o.config_identity_only, 'config'); assert.equal(o.config_identity_only.sha256, previous.isolated_config_sha256);
  identity(o.resource_manifest_identity, 'manifest'); assert.equal(o.resource_manifest_identity.sha256, previous.resource_manifest_sha256);
  const binary = o.on_disk_server_binary;
  keys(binary, ['embedded_settings','go_version','identity','matches_previously_reviewed_build'], 'binary'); identity(binary.identity, 'binary identity');
  same(binary.identity, {bytes: previous.on_disk_server_binary.bytes, sha256: previous.on_disk_server_binary.sha256});
  same(binary.embedded_settings, previous.on_disk_server_binary.embedded_build_settings); assert.equal(binary.go_version, previous.on_disk_server_binary.go_version); assert.equal(binary.matches_previously_reviewed_build, true);
  const process = p => {
    keys(p, ['http_listener_query','matching_processes','state'], 'process observation'); assert.equal(p.state, 'observed'); assert.equal(p.http_listener_query, 'observed');
    assert(Array.isArray(p.matching_processes) && p.matching_processes.length === 1, 'ambiguous selected listener');
    const x = p.matching_processes[0]; keys(x, ['image_matches_selected_binary','owns_selected_http_listener','start_utc'], 'selected process');
    assert.equal(x.image_matches_selected_binary, true); assert.equal(x.owns_selected_http_listener, true); utc(x.start_utc, 'process start');
    assert(Date.parse(x.start_utc) <= Date.parse(o.collected_utc)); return x;
  };
  same(process(o.process_before), process(o.process_after)); assert.equal(o.observed_files_stable_on_recheck, true);
  keys(o.startup_script_identity, ['identity','scope','state'], 'startup script'); identity(o.startup_script_identity.identity, 'startup script identity'); assert.equal(o.startup_script_identity.state, 'hashed_only');
  assert(Array.isArray(o.tables) && o.tables.length === 29 && o.total_tables === 29 && o.matched_tables === 29);
  const names = new Set();
  for (const t of o.tables) {
    keys(t, ['identity','matches_expected','name','state'], 'table'); assert.equal(t.state, 'hashed_only'); assert.equal(t.matches_expected, true); assert(!names.has(t.name), 'duplicate table name'); names.add(t.name);
    identity(t.identity, 'table identity'); const e = expected.tables.find(x => x.name === t.name); assert(e, 'unknown table name'); same(t.identity, {bytes: e.bytes, sha256: e.sha256});
  }
  assert.equal(names.size, expected.tables.length);
  assert(Array.isArray(o.static_resources) && o.static_resources.length === 2);
  const roles = new Set();
  for (const r of o.static_resources) {
    keys(r, ['projection_contains','raw_replay_count','role','source_identity','state'], 'static source'); identity(r.source_identity, 'static source identity');
    assert(['battle','normal_quest'].includes(r.role) && !roles.has(r.role)); roles.add(r.role); assert.equal(r.state, 'pinned_static_projection'); assert.equal(typeof r.projection_contains, 'string');
    assert.equal(r.raw_replay_count, r.role === 'battle' ? 3102 : 50);
    if (r.role === 'battle') assert.equal(r.source_identity.sha256, previous.runtime_static_projection.source_sha256);
  }
  return {tablesCompared: names.size, beforeAfterProcessReportsAgree: true, reportedProcessStartUtc: o.process_before.matching_processes[0].start_utc,
    selectedListenerPort: o.selected_http_port, observedDiskIdentitiesMatchPriorDelivery: true,
    effectiveLoadedBytes: 'UNKNOWN', deployedSourceVersion: 'UNKNOWN', sourceClientAndConversion: 'UNKNOWN', publicationAndAdmission: 'UNKNOWN'};
}

const replayKeys = ['boss_id','enemy_party_id','enemy_type','battles','cost_initial','burst_gauge_initial','hold_max','end_turn'];
function replaySet(rows, label) {
  assert(Array.isArray(rows)); const map = unique(rows, 'boss_id', label);
  for (const r of rows) {
    keys(r, Object.hasOwn(r, 'battles') ? replayKeys : replayKeys.filter(k => k !== 'battles'), label);
    for (const key of replayKeys.filter(k => k !== 'battles')) integer(r[key], key, key === 'boss_id' || key === 'enemy_party_id' ? 1 : 0);
    assert(r.enemy_type <= 4);
    if (Object.hasOwn(r, 'battles')) {
      assert(Array.isArray(r.battles) && r.battles.length > 0 && r.battles.length <= 100);
      for (const b of r.battles) { keys(b, ['enemy_party_id','enemy_type'], 'wave'); integer(b.enemy_party_id, 'party', 1); integer(b.enemy_type, 'enemy type'); assert(b.enemy_type <= 4); }
      assert.equal(r.enemy_party_id, r.battles[0].enemy_party_id); assert.equal(r.enemy_type, r.battles[0].enemy_type);
    }
  } return map;
}
function entrySet(groups, label) {
  assert(Array.isArray(groups)); const result = new Map(); unique(groups, '0', label);
  for (const g of groups) {
    keys(g, ['0','1','10'], label); integer(g['1'], 'group stage type'); assert(Array.isArray(g['10']) && g['10'].length > 0);
    for (const q of g['10']) {
      keys(q, ['0','1','5','7','24'], 'quest entry'); integer(q['0'], 'boss ID', 1); integer(q['5'], 'BP cost');
      assert([0,1].includes(q['1']) && [0,1].includes(q['7']) && [0,1,2,3].includes(q['24']), 'invalid static entry rule');
      assert(!result.has(q['0']), 'duplicate quest entry'); result.set(q['0'], {groupId: g['0'], groupStageType: g['1'], ...q});
    }
  } return result;
}
const rewardKeys = ['boss_id','stage_quest_area_id','stage_quest_stage_id','tower_id','tower_floor'];
function rewards(rows, replays, label) {
  assert(Array.isArray(rows)); const map = new Map(), contexts = new Set();
  for (const r of rows) {
    keys(r, rewardKeys, label); for (const k of rewardKeys) integer(r[k], k, k === 'boss_id' ? 1 : 0);
    assert(replays.has(r.boss_id), 'reward without static replay'); const context = rewardKeys.map(k => r[k]).join(':'); assert(!contexts.has(context), 'duplicate reward context'); contexts.add(context);
    if (!map.has(r.boss_id)) map.set(r.boss_id, []); map.get(r.boss_id).push(r);
  } assert.equal(map.size, replays.size); return map;
}

export function validateStaticProjections(battle, normal, oldReplays) {
  keys(battle, ['groups','replays','own_deck_variants','rewards','deduplicated_families'], 'battle projection');
  keys(normal, ['entry_groups','replays','rewards'], 'normal projection');
  const original = replaySet(oldReplays, 'previous battle replays'), battleReplays = replaySet(battle.replays, 'battle replays'), normalReplays = replaySet(normal.replays, 'normal replays');
  assert.equal(battleReplays.size, 3102); assert.equal(original.size, 3102); assert.equal(normalReplays.size, 50);
  for (const [id, r] of battleReplays) { assert(original.has(id), 'new battle replay ID'); same(r, original.get(id)); }
  for (const id of normalReplays.keys()) assert(!battleReplays.has(id), 'normal/battle replay ID collision');
  const battleEntries = entrySet(battle.groups, 'battle groups'), normalEntries = entrySet(normal.entry_groups, 'normal groups');
  assert.equal(battle.groups.length, 854); assert.equal(battleEntries.size, 2744); assert.equal(normal.entry_groups.length, 18); assert.equal(normalEntries.size, 50);
  for (const id of battleEntries.keys()) assert(battleReplays.has(id), 'battle entry lacks replay');
  for (const [id, q] of normalEntries) { assert(normalReplays.has(id), 'normal entry lacks replay'); assert(!battleEntries.has(id), 'entry collision'); }
  const battleRewards = rewards(battle.rewards, battleReplays, 'battle reward context'), normalRewards = rewards(normal.rewards, normalReplays, 'normal reward context');
  assert.equal(battle.rewards.length, 3102); assert.equal(normal.rewards.length, 100);
  for (const [id, rs] of normalRewards) {
    assert.equal(rs.length, 2); const group = normalEntries.get(id).groupId;
    assert(rs.some(r => r.stage_quest_area_id === 0 && r.stage_quest_stage_id === 0 && r.tower_id === 0 && r.tower_floor === 0));
    assert(rs.some(r => r.stage_quest_area_id === group && r.stage_quest_stage_id === id && r.tower_id === 0 && r.tower_floor === 0));
  }
  const aliases = new Map(); assert(Array.isArray(battle.deduplicated_families) && battle.deduplicated_families.length === 48);
  unique(battle.deduplicated_families, 'duplicate_family_id', 'duplicate family');
  for (const f of battle.deduplicated_families) {
    keys(f, ['canonical_family_id','duplicate_family_id','boss_aliases'], 'deduplicated family'); integer(f.canonical_family_id, 'canonical family', 1); assert(Array.isArray(f.boss_aliases));
    for (const a of f.boss_aliases) { keys(a, ['duplicate_boss_id','canonical_boss_id'], 'boss alias'); integer(a.duplicate_boss_id, 'duplicate boss', 1); assert(battleReplays.has(a.canonical_boss_id)); assert(!aliases.has(a.duplicate_boss_id)); aliases.set(a.duplicate_boss_id, a.canonical_boss_id); }
  }
  assert(Array.isArray(battle.own_deck_variants) && battle.own_deck_variants.length === 1043);
  const variants = new Map(), sources = new Set();
  for (const v of battle.own_deck_variants) {
    keys(v, ['source_boss_id','boss_id'], 'own deck variant'); integer(v.source_boss_id, 'source boss', 1); integer(v.boss_id, 'variant boss', 1);
    assert(v.source_boss_id < 100000000 && v.boss_id === v.source_boss_id + 100000000, 'wrong variant offset');
    assert(!variants.has(v.boss_id) && !sources.has(v.source_boss_id), 'duplicate own deck variant');
    assert(!battleReplays.has(v.boss_id) && !normalReplays.has(v.boss_id) && !aliases.has(v.boss_id), 'variant ID collision');
    const source = battleEntries.get(v.source_boss_id), reward = battleRewards.get(v.source_boss_id);
    assert(source && source['1'] === 0 && source['24'] === 0 && reward?.length === 1 && reward[0].stage_quest_area_id === 0 && reward[0].tower_id === 0, 'variant source not standalone/normal');
    variants.set(v.boss_id, {sourceBossId: v.source_boss_id, ...source, '0': v.boss_id, '1': 1, '24': 1}); sources.add(v.source_boss_id);
  }
  return {battleReplays, normalReplays, battleEntries, normalEntries, variants, aliasCount: aliases.size,
    stats: {unchangedBattleReplays: battleReplays.size, battleEntryGroups: battle.groups.length, battleEntryBossIds: battleEntries.size,
      ownDeckVariants: variants.size, expandedBattleEntryBossIds: battleEntries.size + variants.size,
      expandedBattleReplays: battleReplays.size + variants.size, normalEntryGroups: normal.entry_groups.length, normalEntryBossIds: normalEntries.size,
      normalReplays: normalReplays.size, normalRewardContexts: normal.rewards.length, normalRewardBossIds: normalRewards.size,
      deduplicatedFamilies: battle.deduplicated_families.length, deduplicatedBossAliases: aliases.size}};
}
