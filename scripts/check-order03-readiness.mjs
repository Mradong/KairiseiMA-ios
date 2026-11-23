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
const acceptance = load('evidence/acceptance.json');
if (!ios.success || !modern.success || !ios.independentCsharpAgreement || !modern.independentCsharpAgreement || !modern.candidateEnumAgreement) throw Error('Metadata verification failed');
if (compatibility.metadataEvidenceSha256 !== sha(p + 'order03-ios602-enums.json')) throw Error('Compatibility report is stale');
if (ios.csharpReaderSha256 !== sha('tests/Order03MetadataEnumsProbe.cs') || modern.independentReaderSha256 !== sha('tests/Order03MetadataEnumsProbe.cs')) throw Error('Independent reader changed');
if (!numericDefaults.staticChecksPassed || numericDefaults.scriptSha256 !== sha('scripts/audit-order03-modern-defaults.mjs') || numericDefaults.source.metadataSha256 !== modern.source.metadataSha256) throw Error('Modern numeric-default evidence is missing or stale');
if (!collector.success || collector.collectorSha256 !== sha('scripts/Collect-Order03RuntimeEvidence.ps1')) throw Error('Collector preparation evidence is missing or stale');
const gate = (id, state, evidence, remaining) => ({id, state, evidence, remaining});
const gates = [
  gate('delivered_tables', delivery.archive.tableEntriesVerified === 29 && delivery.archive.tableMismatches === 0 && delivery.archive.packageMismatches === 0 ? 'verified' : 'failed', [p + 'order03-server-delivery-validation.json'], 'Recorded package verification of 29 original tables; does not assert current process memory.'),
  gate('specified_positive_id_references', delivery.reproducedOfflineAudit.positiveReferencesChecked === 446725 && delivery.reproducedOfflineAudit.missingReferences === 0 ? 'verified' : 'failed', [p + 'order03-server-delivery-validation.json'], 'Specified positive ID references only; gameplay and symbolic semantics are separate.'),
  gate('selected_enum_number_sources', 'verified', [p + 'order03-ios602-enums.json', p + 'order03-modern-metadata-enums.json'], '23 families each, 670 original iOS constants and 822 original Windows rebuild constants; not a claim that the CSV cohort shares an official original version.'),
  gate('data_cohort_and_behavior_compatibility', 'partial', [p + 'order03-ios602-table-compatibility.json', p + 'order03-modern-metadata-enums.json', p + 'order03-modern-defaults.json', p + 'order03-value-parser-differential.json', p + 'order03-short-ai-tail.json'], 'Modern candidate numbers and original Windows numeric-default paths have metadata/binary evidence. These are static Windows observations, not 25-value/36-row IL2CPP execution, full CSV/AI or original iOS semantics. Link the delivered CSV version/transformation to its intended client/rebuild contract; original source-client version remains unverified. Old schema cannot represent 40 checked names; retain the modern scope.'),
  gate('actual_deployed_version_and_quest_scope', 'awaiting_server_evidence', [p + 'order03-server-delivery-validation.json', p + 'order03-quest-variant-gap.json', p + 'order03-public-version-evidence.json', p + 'order03-server-supplement.json', p + 'order03-runtime-collector-tests.json'], 'Public health has no version/snapshot fields. A tested read-only collector and supplemental handoff are prepared, but not executed on the server. Confirm running source/build and effective loaded table/quest snapshot, 1043 own-deck variants and one other quest outside the static projection. Disk paths/hashes and shape matches alone are insufficient.'),
  gate('safe_battle_contract_and_redacted_interface_evidence', 'awaiting_server_evidence', [p + 'order03-isolated-deck-contract.json', p + 'order03-isolated-quest-contract.json'], 'Existing reply documents source-derived start/continue/end side effects and lack of process-restart recovery; actual deployed semantics and safe isolated admission/retry/recovery/settlement examples still missing. No chargeable entry until the client can continue safely.')
];
const report = {checkedAt: new Date().toISOString(), planItem: '03', goalStatus: acceptance.focusedGoal?.status === 'blocked' ? 'blocked' : 'active', ready: gates.every(x => x.state === 'verified'), gates,
  evidenceHashes: [...new Set(gates.flatMap(x => x.evidence))].map(file => ({file, sha256: sha(file)})),
  scope: 'Acceptance ledger, not an automatic certification of runtime versions or unobserved behavior. Historical test success is used only within its stated scope. Order 03 remains incomplete while any gate lacks required evidence; whole-game acceptance is separate.'};
fs.writeFileSync(path.join(root, p + 'order03-readiness.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({planItem: '03', ready: report.ready, gates: gates.map(x => ({id: x.id, state: x.state}))}, null, 2));
