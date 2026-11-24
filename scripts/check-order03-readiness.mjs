import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const load = p => JSON.parse(fs.readFileSync(path.join(root, p), 'utf8').replace(/^\uFEFF/, ''));
const sha = p => crypto.createHash('sha256').update(fs.readFileSync(path.join(root, p))).digest('hex');
const p = 'evidence/ios-package-search/';
const delivery = load(p + 'order03-server-delivery-validation.json');
const ios = load(p + 'order03-ios602-enums.json'), modern = load(p + 'order03-modern-metadata-enums.json');
const numericDefaults = load(p + 'order03-modern-defaults.json');
const compatibility = load(p + 'order03-ios602-table-compatibility.json');
const collector = load(p + 'order03-runtime-collector-tests.json');
const runtimeSupplement = load(p + 'order03-runtime-supplement-validation.json');
const runtimeTests = load(p + 'order03-runtime-supplement-tests.json');
const liveBattle = load(p + 'order03-live-battle-contract.json');
const sourceLink = load(p + 'order03-source-link-reproduction.json');
const acceptance = load('evidence/acceptance.json');
if (!ios.success || !modern.success || !ios.independentCsharpAgreement || !modern.independentCsharpAgreement || !modern.candidateEnumAgreement) throw Error('Metadata verification failed');
if (compatibility.metadataEvidenceSha256 !== sha(p + 'order03-ios602-enums.json')) throw Error('Compatibility report is stale');
if (ios.csharpReaderSha256 !== sha('tests/Order03MetadataEnumsProbe.cs') || modern.independentReaderSha256 !== sha('tests/Order03MetadataEnumsProbe.cs')) throw Error('Independent reader changed');
if (!numericDefaults.staticChecksPassed || numericDefaults.scriptSha256 !== sha('scripts/audit-order03-modern-defaults.mjs') || numericDefaults.source.metadataSha256 !== modern.source.metadataSha256) throw Error('Modern numeric-default evidence is missing or stale');
if (!collector.success || collector.collectorSha256 !== sha('scripts/Collect-Order03RuntimeEvidence.ps1')) throw Error('Collector preparation evidence is missing or stale');
if (!runtimeSupplement.analysisCompleted || runtimeSupplement.archive.sha256 !== 'cf68822b1837e8f61845236c3c170eb535996a1e73cd419a233dc46b0bc3b964' || !runtimeTests.success || runtimeTests.validatorSha256 !== sha('scripts/order03-runtime-supplement-validation.mjs')) throw Error('Runtime supplement verification missing or stale');
if (!liveBattle.success || liveBattle.sideEffects.clearRewardRequested || !liveBattle.steps.some(x => x.phase === 'abandon_and_receipt' && x.chargedExactlyOnce && x.identicalRetrySamePayload)) throw Error('Live battle contract evidence is missing or unsafe');
if (!sourceLink.success || sourceLink.receivedSourceManifest.matchedLocalFiles !== 164 || sourceLink.receivedSourceManifest.mismatches !== 0 || sourceLink.conclusion.observedExecutableExactReproduction !== false) throw Error('Source-link reproduction evidence is missing or inconsistent');
for (const source of runtimeSupplement.sourceHashes.filter(x => x.file.startsWith('scripts/'))) if (source.sha256 !== sha(source.file)) throw Error('Runtime supplement audit source changed');
const gate = (id, state, evidence, remaining) => ({id, state, evidence, remaining});
const gates = [
  gate('delivered_tables', delivery.archive.tableEntriesVerified === 29 && delivery.archive.tableMismatches === 0 && delivery.archive.packageMismatches === 0 ? 'verified' : 'failed', [p + 'order03-server-delivery-validation.json'], 'Recorded package verification of 29 original tables; does not assert current process memory.'),
  gate('specified_positive_id_references', delivery.reproducedOfflineAudit.positiveReferencesChecked === 446725 && delivery.reproducedOfflineAudit.missingReferences === 0 ? 'verified' : 'failed', [p + 'order03-server-delivery-validation.json'], 'Specified positive ID references only; gameplay and symbolic semantics are separate.'),
  gate('selected_enum_number_sources', 'verified', [p + 'order03-ios602-enums.json', p + 'order03-modern-metadata-enums.json'], '23 families each, 670 original iOS constants and 822 original Windows rebuild constants; not a claim that the CSV cohort shares an official original version.'),
  gate('data_cohort_and_behavior_compatibility', 'partial', [p + 'order03-ios602-table-compatibility.json', p + 'order03-modern-metadata-enums.json', p + 'order03-modern-defaults.json', p + 'order03-value-parser-differential.json', p + 'order03-short-ai-tail.json'], 'Modern candidate numbers and original Windows numeric-default paths have metadata/binary evidence. These are static Windows observations, not 25-value/36-row IL2CPP execution, full CSV/AI or original iOS semantics. Link the delivered CSV version/transformation to its intended client/rebuild contract; original source-client version remains unverified. Old schema cannot represent 40 checked names; retain the modern scope.'),
  gate('received_disk_and_listener_observation', 'verified', [p + 'order03-runtime-supplement-import.json', p + 'order03-runtime-supplement-validation.json'], 'Verified scope: received server report of 29 unchanged disk tables, executable/config/manifest identity and same reported process start/image-path/listener association before/after. Client did not inspect the server; loaded configuration/bytes and source commit remain UNKNOWN.'),
  gate('received_static_quest_mapping', 'verified', [p + 'order03-runtime-supplement-validation.json', p + 'order03-runtime-supplement-tests.json'], 'Verified scope: 3102 battle replays equal prior projection; 2744 base entries and 1043 explicit own-deck mappings; 50 normal quests/228 waves with specified ID references present. Historical 3788 aggregate count agrees, but exact historical normal/variant membership and live publication/admission remain UNKNOWN.'),
  gate('reviewed_source_and_observed_binary_link', 'partial', [p + 'order03-source-link-reproduction.json'], 'All 164 received reviewed battle-source hashes match local files and targeted local recovery/result tests pass. Rebuilding base d1a202eb plus the reviewed iOS patch with Go 1.25.1 under ordinary and stripped variants did not reproduce server executable 7704963e; deployed source/patch relation and effective loaded configuration remain UNKNOWN.'),
  gate('actual_deployed_version_and_quest_scope', 'partial', [p + 'order03-server-delivery-validation.json', p + 'order03-quest-variant-gap.json', p + 'order03-runtime-supplement-validation.json', p + 'order03-live-battle-contract.json'], 'Live account evidence now establishes publication/admission for candidate 30010102 at the observed service, including one real Start. Effective loaded snapshot/configuration, deployed source/patch relation, original CSV transformation and exact historical list membership remain UNKNOWN. Do not infer them from disk/path/count matches.'),
  gate('safe_battle_contract_and_redacted_interface_evidence', 'partial', [p + 'order03-isolated-deck-contract.json', p + 'order03-isolated-quest-contract.json', p + 'order03-live-battle-contract.json'], 'A dedicated live isolated account verified candidate admission, one 15-BP debit, identical Start retry, relog recovery, rejected no-charge Continue, non-clear End/abandon and identical End retry without clear reward. Successful paid/free Continue, real engine-produced victory/defeat logs, clear settlement/reward idempotency and actual process-restart recovery remain unverified; do not fabricate a victory to fill them.')
];
const report = {checkedAt: new Date().toISOString(), planItem: '03', goalStatus: ['blocked','paused'].includes(acceptance.focusedGoal?.status) ? acceptance.focusedGoal.status : 'active', ready: gates.every(x => x.state === 'verified'), gates,
  evidenceHashes: [...new Set(gates.flatMap(x => x.evidence))].map(file => ({file, sha256: sha(file)})),
  scope: 'Acceptance ledger, not an automatic certification of runtime versions or unobserved behavior. Historical test success is used only within its stated scope. Order 03 remains incomplete while any gate lacks required evidence; whole-game acceptance is separate.'};
fs.writeFileSync(path.join(root, p + 'order03-readiness.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({planItem: '03', ready: report.ready, gates: gates.map(x => ({id: x.id, state: x.state}))}, null, 2));
