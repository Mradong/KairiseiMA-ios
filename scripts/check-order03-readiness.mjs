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
const remainingCapturePath = p + 'order03-remaining-validation.json';
const remainingCapture = fs.existsSync(path.join(root, remainingCapturePath)) ? load(remainingCapturePath) : null;
const rawLoaderPath = p + 'order03-raw-static-loader.json';
const rawLoader = fs.existsSync(path.join(root, rawLoaderPath)) ? load(rawLoaderPath) : null;
const reviewedPatchPath = p + 'order03-reviewed-patch-reproduction.json';
const reviewedPatch = fs.existsSync(path.join(root, reviewedPatchPath)) ? load(reviewedPatchPath) : null;
const selectionPath = p + 'order03-runtime-selection-validation.json';
const selection = fs.existsSync(path.join(root, selectionPath)) ? load(selectionPath) : null;
const realBattlePath = p + 'order03-real-battle-validation.json';
const realBattle = fs.existsSync(path.join(root, realBattlePath)) ? load(realBattlePath) : null;
const publicationPath = p + 'order03-publication-provenance-lyd.json';
const publication = fs.existsSync(path.join(root, publicationPath)) ? load(publicationPath) : null;
const restartResultPath = p + 'order03-restart-result-20261125T025522528Z-validation-lyd.json';
const restartResult = fs.existsSync(path.join(root, restartResultPath)) ? load(restartResultPath) : null;
const publishedSnapshotPath = p + 'order03-published-client-snapshot-lyd.json';
const publishedSnapshot = fs.existsSync(path.join(root, publishedSnapshotPath)) ? load(publishedSnapshotPath) : null;
const latestRestartPath = p + 'order03-restart-result-20261125T031514670Z-validation-lyd.json';
const latestRestart = fs.existsSync(path.join(root, latestRestartPath)) ? load(latestRestartPath) : null;
const startupPinTestsPath = p + 'order03-startup-pin-tests-lyd.json';
const startupPinPackagePath = p + 'order03-startup-pin-package-lyd.json';
const startupPinTests = fs.existsSync(path.join(root,startupPinTestsPath)) ? load(startupPinTestsPath) : null;
const startupPinPackage = fs.existsSync(path.join(root,startupPinPackagePath)) ? load(startupPinPackagePath) : null;
if (startupPinTests && (!startupPinTests.success || startupPinTests.scriptSha256 !== sha('scripts/Sync-Order03StartupPin.ps1') || startupPinTests.startupSource.sha256 !== sha(startupPinTests.startupSource.file))) throw Error('Startup pin tests missing or stale');
if (startupPinPackage && startupPinPackage.archiveSha256 !== crypto.createHash('sha256').update(fs.readFileSync(startupPinPackage.archive)).digest('hex')) throw Error('Startup pin package changed');
if (publishedSnapshot) {
  if (!publishedSnapshot.success || publishedSnapshot.scriptSha256 !== sha('scripts/audit-order03-published-client-snapshot.mjs') || publishedSnapshot.snapshot.sha256 !== sha(publishedSnapshot.snapshot.file)) throw Error('Published client/snapshot evidence missing or stale');
  for (const input of publishedSnapshot.inputFiles) if (input.sha256 !== sha(input.file)) throw Error('Published client/snapshot input changed');
}
if (latestRestart) {
  if (!latestRestart.analysisCompleted || latestRestart.scriptSha256 !== sha('scripts/audit-order03-restart-latest.mjs')) throw Error('Latest restart audit missing or stale');
  for (const input of latestRestart.inputFiles) if (input.sha256 !== sha(input.file)) throw Error('Latest restart input changed');
  if (latestRestart.sourceBoundary.sha256 !== sha(latestRestart.sourceBoundary.evidence)) throw Error('Restart source boundary evidence changed');
}
if (publication) {
  if (!publication.success || publication.tableCount !== 29 || publication.tableMismatches !== 0 || !publication.publishedResourceManifestMatchesSelectedRuntime || publication.scriptSha256 !== sha('scripts/audit-order03-publication-provenance.mjs')) throw Error('Publication provenance evidence missing or stale');
  for (const input of publication.inputFiles) if (input.sha256 !== sha(input.file)) throw Error('Publication provenance input changed');
  for (const table of publication.tables) if (table.sha256 !== sha(table.receivedFile)) throw Error('Published table identity no longer matches received bytes');
}
if (restartResult) {
  if (!restartResult.analysisCompleted || restartResult.scriptSha256 !== sha('scripts/audit-order03-restart-result.mjs')) throw Error('Restart result analysis missing or stale');
  for (const input of restartResult.inputFiles) if (input.sha256 !== sha(input.file)) throw Error('Restart result input changed');
}
if (realBattle) {
  if (!realBattle.success || realBattle.scriptSha256 !== sha('scripts/audit-order03-real-battle.mjs') || realBattle.sourceReport.sha256 !== sha(realBattle.sourceReport.file)) throw Error('Real battle evidence missing or stale');
  for (const log of realBattle.logs) if (log.sha256 !== sha(log.file)) throw Error('Real battle log changed');
}
if (selection && (!selection.success || !selection.selectedRuntimeInputsVerified || selection.scriptSha256 !== sha('scripts/audit-order03-runtime-selection.mjs') || selection.inputFile.sha256 !== sha(selection.inputFile.file))) throw Error('Runtime selection evidence missing or stale');
if (reviewedPatch) {
  if (!reviewedPatch.analysisCompleted || !reviewedPatch.allBaseAndPatchedGitBlobIdentitiesMatched || !reviewedPatch.build.exactObservedBinaryReproduction || reviewedPatch.scriptSha256 !== sha('scripts/reproduce-order03-reviewed-patch.mjs') || reviewedPatch.inputPatch.sha256 !== sha(reviewedPatch.inputPatch.file)) throw Error('Reviewed patch reproduction evidence missing or stale');
  if (reviewedPatch.build.sha256 !== sha(reviewedPatch.build.executable)) throw Error('Reproduced executable changed');
}
if (rawLoader) {
  if (!rawLoader.success || !rawLoader.allLoadedOwnDeckMappingsIndependentlyReproduced || rawLoader.probeSha256 !== sha('tests/Order03RawStaticProbe.go') || rawLoader.scriptSha256 !== sha('scripts/test-order03-raw-static-loader.mjs')) throw Error('Raw static loader evidence missing or stale');
  for (const source of rawLoader.loaderSources) if (source.sha256 !== sha(source.file)) throw Error('Raw static loader source changed');
}
if (remainingCapture) {
  if (!remainingCapture.success || !remainingCapture.rawStaticProjection.reproduced || remainingCapture.auditSourceSha256 !== sha('scripts/audit-order03-remaining.mjs')) throw Error('Latest server capture validation missing or stale');
  for (const input of remainingCapture.inputFiles) if (input.sha256 !== sha(input.file)) throw Error('Latest server capture input changed');
}
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
if (remainingCapture) {
  for (const id of ['received_disk_and_listener_observation', 'received_static_quest_mapping']) gates.find(g => g.id === id).evidence.push(remainingCapturePath);
  gates.find(g => g.id === 'received_static_quest_mapping').remaining = 'Latest original static JSON reproduces all previously retained battle and normal-quest projection fields. Exact historical list membership, complete extra raw-field semantics and live behavior remain unverified.';
  gates.find(g => g.id === 'received_disk_and_listener_observation').remaining = 'Latest received capture reports the same executable/config/manifest/table hashes and a new process start time. Actual process invocation, effective loaded content and deployment source remain UNKNOWN; process restart alone does not prove battle recovery.';
}
if (rawLoader) gates.find(g => g.id === 'received_static_quest_mapping').evidence.push(rawLoaderPath);
if (reviewedPatch) {
  const sourceGate = gates.find(g => g.id === 'reviewed_source_and_observed_binary_link');
  sourceGate.state = 'verified';
  sourceGate.evidence.push(reviewedPatchPath);
  sourceGate.remaining = 'The received reviewed patch plus pinned base d1a202eb, Go 1.25.1 and recorded flags reproduces the reported server disk executable SHA-256 exactly. Actual configuration/table selection and original CSV extraction remain separate evidence gaps.';
}
if (selection) {
  const runtimeGate = gates.find(g => g.id === 'actual_deployed_version_and_quest_scope');
  runtimeGate.evidence.push(selectionPath);
  runtimeGate.remaining = 'Received process arguments select the reproduced executable resource manifest, matching 29 tables/static data and iOS profile; source loads the catalog before listening. Mutable publication and exact historical membership remain unverified.';
}
if (realBattle) {
  const contractGate = gates.find(g => g.id === 'safe_battle_contract_and_redacted_interface_evidence');
  contractGate.evidence.push(realBattlePath);
  contractGate.remaining = 'Dedicated identities and actual service party/table inputs produced accepted engine victory, paid Continue with one 50-crystal debit and matching retry, engine revival, real defeat and delayed frozen-result retry. Remaining for the current targeted verification: actual service process-restart recovery. Wider gameplay/device work belongs to later plan rows.';
}
if (publication) {
  gates.push(gate('published_project_table_cohort', 'verified', [publicationPath], 'All 29 received table byte identities match the recovered-project 2.0.4 published release manifest. The published resource manifest exactly matches the selected runtime manifest. Only ZIP directory/manifests were downloaded; the full archive digest was not checked. Official original-client extraction input/tool/transform remains UNKNOWN.'));
  const cohortGate = gates.find(g => g.id === 'data_cohort_and_behavior_compatibility');
  cohortGate.evidence.push(publicationPath);
  cohortGate.remaining = 'Recovered-project 2.0.4 publication, pinned base commit, selected resource manifest and all 29 table identities now match. Original official-client extraction/version/transform and complete original gameplay semantics remain UNKNOWN; selected modern metadata and static compatibility retain their stated scope.';
}
if (restartResult) {
  const contractGate = gates.find(g => g.id === 'safe_battle_contract_and_redacted_interface_evidence');
  contractGate.evidence.push(restartResultPath);
  contractGate.remaining += ' Latest received attempt timed out at initial login before character creation or battle entry. Submitted preflight records a start request after the failure, not readiness or a proven timeout cause.';
}
if (publishedSnapshot) {
  const cohortGate = gates.find(g => g.id === 'data_cohort_and_behavior_compatibility');
  cohortGate.evidence.push(publishedSnapshotPath);
  cohortGate.state = 'verified';
  cohortGate.remaining = 'Scoped to recovered-project 2.0.4: published server table identities and exact public Windows metadata now bind all 29 snapshot tables and 23/822 enum values to the same release. All 20 required tables are present. Official original-client extraction/transform and complete AI/skill/device behavior remain explicit limitations; they are not asserted by this version-cohort evidence.';
  gates.push(gate('real_snapshot_byte_integrity', 'verified', [publishedSnapshotPath], 'Existing isolated CombatTables.json is 57,738,263 bytes, with exact published CSV byte hashes and independently decoded 822 enum values. Managed transport source matches the validated version. Snapshot remains isolated pending service recovery/platform work.'));
}
if (selection && reviewedPatch && publication && realBattle) {
  const runtimeGate = gates.find(g => g.id === 'actual_deployed_version_and_quest_scope');
  runtimeGate.state = 'verified';
  runtimeGate.remaining = 'Verified within the recorded observations: reproduced executable, explicit runtime-selected public manifest/table/static/iOS-profile identities, loader startup order and actual admission for 30010102. Mutable publication, exact historical list membership and complete device gameplay are not inferred.';
}
if (latestRestart) {
  const contractGate = gates.find(g => g.id === 'safe_battle_contract_and_redacted_interface_evidence');
  contractGate.evidence.push(latestRestartPath, latestRestart.sourceBoundary.evidence);
  contractGate.remaining = 'Actual process restart and identity-checked relogin passed, but Start restoration failed. Reviewed deployed source and local SQLite/HTTP fixtures show that solo openings were memory-only while BP persisted. The failed restore HTTP status was overwritten by cleanup and remains NOT_CAPTURED. A separately compiled durable-solo fix has local tests; actual installation/startup/restart recovery must be verified on the independent service.';
}
if (startupPinTests && startupPinPackage) {
  const contractGate = gates.find(g => g.id === 'safe_battle_contract_and_redacted_interface_evidence');
  contractGate.evidence.push(startupPinTestsPath,startupPinPackagePath);
  contractGate.remaining += ' Installed binary was blocked by the retained old expected_test_binary_sha256. A reviewed one-field sync preserves original preflight checks and passes real-checker fixtures; actual sync/startup/restart receipts are pending.';
}
const report = {checkedAt: new Date().toISOString(), planItem: '03', goalStatus: ['blocked','paused'].includes(acceptance.focusedGoal?.status) ? acceptance.focusedGoal.status : 'active', ready: gates.every(x => x.state === 'verified'), gates,
  evidenceHashes: [...new Set(gates.flatMap(x => x.evidence))].map(file => ({file, sha256: sha(file)})),
  scope: 'Acceptance ledger, not an automatic certification of runtime versions or unobserved behavior. Historical test success is used only within its stated scope. Order 03 remains incomplete while any gate lacks required evidence; whole-game acceptance is separate.'};
fs.writeFileSync(path.join(root, p + 'order03-readiness.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({planItem: '03', ready: report.ready, gates: gates.map(x => ({id: x.id, state: x.state}))}, null, 2));
