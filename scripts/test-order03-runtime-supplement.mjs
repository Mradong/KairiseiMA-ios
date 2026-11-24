import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {validateRuntimeObservation, validateStaticProjections} from './order03-runtime-supplement-validation.mjs';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = p => fs.readFileSync(path.join(root, p));
const json = p => JSON.parse(read(p).toString('utf8').replace(/^\uFEFF/, ''));
const input = 'inputs/server-order03/Order03-Runtime-Supplement-20261124T010145.658885200Z/';
const o = json(input + 'runtime-observation.json'), b = json(input + 'battle-static-projection.json'), n = json(input + 'normal_quest-static-projection.json');
const expected = json('evidence/ios-package-search/order03-expected-tables.json'), old = json('inputs/server-order03/Order03-Combat-Verified-20261123/collection-manifest.json');
const replays = json('inputs/server-order03/Order03-Combat-Verified-20261123/runtime-static-replays.json');
const clone = x => JSON.parse(JSON.stringify(x));
const cases = [];
validateRuntimeObservation(o, expected, old); validateStaticProjections(b, n, replays);
cases.push({case: 'received_three_file_structures', passed: true});
function rejectsObservation(name, mutate) { const changed = clone(o); mutate(changed); assert.throws(() => validateRuntimeObservation(changed, expected, old)); cases.push({case: name, rejected: true}); }
function rejectsStatic(name, mutate) { const changed = clone(b), normal = clone(n); mutate(changed, normal); assert.throws(() => validateStaticProjections(changed, normal, replays)); cases.push({case: name, rejected: true}); }
rejectsObservation('same_table_name_repeated', x => x.tables[1] = clone(x.tables[0]));
rejectsObservation('declared_match_with_wrong_hash', x => x.tables[0].identity.sha256 = '0'.repeat(64));
rejectsObservation('nonzero_business_requests', x => x.business_requests = 1);
rejectsObservation('listener_not_owned', x => x.process_after.matching_processes[0].owns_selected_http_listener = false);
rejectsObservation('different_process_start', x => x.process_after.matching_processes[0].start_utc = '2026-11-21T00:00:00Z');
rejectsObservation('unsubstantiated_loaded_bytes_pass', x => x.effective_loaded_bytes = 'PASS');
rejectsStatic('replay_opening_changed', x => x.replays[0].cost_initial++);
rejectsStatic('wrong_variant_offset', x => x.own_deck_variants[0].boss_id++);
rejectsStatic('variant_source_missing', x => x.own_deck_variants[0].source_boss_id = 12345678);
rejectsStatic('duplicate_variant', x => x.own_deck_variants[1] = clone(x.own_deck_variants[0]));
rejectsStatic('entry_without_replay', x => x.groups[0]['10'][0]['0'] = 12345678);
rejectsStatic('duplicate_normal_reward_context', (_, x) => x.rewards[1] = clone(x.rewards[0]));
rejectsStatic('unknown_projection_field', x => x.extra = 'unreviewed');
const hash = p => crypto.createHash('sha256').update(read(p)).digest('hex');
const report = {checkedAt: new Date().toISOString(), success: true, cases,
  validatorSha256: hash('scripts/order03-runtime-supplement-validation.mjs'), scriptSha256: hash('scripts/test-order03-runtime-supplement.mjs'),
  scope: 'Offline verifier checks using received static data and explicitly mutated in-memory copies. Valid receipt structures accepted, tampered identities/mappings/contexts and stronger unsupported loaded-byte claims rejected. Tests validate audit guards, not independent server observation or runtime compatibility; original inputs are not modified.'};
fs.writeFileSync(path.join(root, 'evidence/ios-package-search/order03-runtime-supplement-tests.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify(report, null, 2));
