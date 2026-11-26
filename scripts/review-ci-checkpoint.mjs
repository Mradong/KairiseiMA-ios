import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const dir = path.join(root, 'ci-upload');
const file = path.join(dir, 'ci-source-manifest.json');
const manifest = JSON.parse(fs.readFileSync(file, 'utf8'));
const extra = ['evidence/native/registered-enemy-user-attack.txt', '安装到iPhone.md', 'evidence/native/registered-enemy-normal-action.txt', 'evidence/ios-package-search/ipa-client004-delivery.json', 'evidence/ios-package-search/ios-client004-cloud-review.json', 'evidence/ios-package-search/ios-client004-artifact-verification.json', 'evidence/ios-package-search/ios-client004-cloud-request.json', 'evidence/ios-package-search/ios-client004-cloud-status.json', 'evidence/ios-package-search/ios-quest-list-test.log', 'evidence/ios-package-search/ios-quest-isolated-fixture.json', 'scripts/Get-IosClient004CloudBuild.ps1', 'scripts/Verify-IosClient004IPA.ps1', 'evidence/ios-package-search/ios-deck-save-test.log', 'evidence/ios-package-search/ios-deck-request-fixture.json', 'evidence/ios-package-search/ios-card-flow-test.log', 'evidence/ios-package-search/ios-cards-isolated-fixture.json', 'server-ios/server/internal/httpapi/ios_bonus_projection_test.go', 'evidence/ios-package-search/ios-bonus-projection-test.log', 'evidence/ios-package-search/ios-bonus-projection-fixture.json', 'server-ios/server/internal/cnbootstrap/ios_character_flow_test.go', 'evidence/ios-package-search/ios-character-home-test.log', 'evidence/ios-package-search/ios-home-isolated-fixture.json', 'evidence/ios-package-search/ios-character-flow-test.log', 'tests/CnProtocolTests.cs', 'scripts/run-tests.mjs', 'evidence/protocol-tests.json', 'tests/SkillQueryDifferential.cs', 'tests/PreparedSkillQueryDifferential.cs', 'tests/PreparedCardQueryDifferential.cs', 'tests/RegisteredSkillQueryDifferential.cs', 'tests/PreparedEnemyQueryDifferential.cs', 'tests/RegisteredBattleDifferential.cs',
 'tests/fixtures/native-session/tables.json', 'tests/fixtures/native-session/battle.json',
 'scripts/test-native-skill-query.mjs', 'scripts/test-prepared-skill-query.mjs', 'scripts/test-prepared-card-query.mjs', 'scripts/test-registered-skill-query.mjs', 'scripts/test-prepared-enemy-query.mjs', 'scripts/test-registered-battle.mjs',
 'scripts/Build-NativeBridge.ps1', 'native/bridge/skill_query_test.go', 'native/bridge/skill_wire_test.go', 'native/bridge/registered_enemy_test.go',
 'evidence/native/skill-query-differential-tests.json', 'evidence/native/prepared-skill-query-differential-tests.json',
 'evidence/native/registration-state-tests.json', 'evidence/native/go-symbols-summary.json', 'evidence/native/prepared-card-query-differential-tests.json', 'evidence/native/registered-skill-query-differential-tests.json', 'evidence/native/prepared-enemy-query-differential-tests.json', 'evidence/native/registered-battle-differential-tests.json', 'evidence/native/session-differential-tests.json', 'evidence/native/implementation-status.json',
 'evidence/native/parameter-differential-tests.json', 'evidence/native/preparation-differential-tests.json', 'evidence/native/registration-differential-tests.json',
 'evidence/original-input-verification.json', 'evidence/acceptance.json', 'STATUS.md'];
extra.push('evidence/native/registered-enemy-role-attacks.txt');
extra.push('evidence/native/registered-enemy-slot1-action.txt');
extra.push('evidence/native/registered-enemy-skill-slots.txt');
extra.push('server-ios/server/internal/httpapi/solo_battle_start_test.go', 'evidence/ios-package-search/ios-solo-start-test.log', 'evidence/ios-package-search/ios-solo-start-fixture.json', 'evidence/ios-package-search/ios-solo-start-request.json');
extra.push('server-ios/server/internal/httpapi/partner_recommendations_test.go', 'evidence/ios-package-search/ios-partner-flow-test.log', 'evidence/ios-package-search/ios-partner-isolated-fixture.json', 'evidence/ios-package-search/ios-runtime-api-check.json');
extra.push('EXECUTION_PLAN.md', '新窗口接手.md');
extra.push('服务端协作需求.md');
extra.push('scripts/audit-602-scenes.mjs', 'scripts/test-602-scene-audit.mjs', 'evidence/ios-package-search/ios602-scene-audit.json', 'evidence/feature-coverage.md');
extra.push('scripts/audit-602-home.mjs', 'evidence/ios-package-search/ios602-home-resource-audit.json');
extra.push('ios-client/Assets/Recovery/Runtime/RecoveryAtlas.cs', 'tests/RecoveryAtlasTests.cs', 'scripts/test-recovery-atlas.mjs', 'evidence/ios-package-search/ios-atlas-tests.json');
extra.push('scripts/Get-IosClient005CloudBuild.ps1', 'scripts/Verify-IosClient005IPA.ps1');
extra.push('scripts/Test-IosClient005Download.ps1', 'evidence/ios-package-search/ios-client005-download-test.json');
extra.push('evidence/native/registered-managed-configured-start.txt');
extra.push('evidence/native/registered-managed-service-turn-start.txt');
extra.push('evidence/native/registered-service-partners.txt');
extra.push('evidence/native/registered-service-card-registration.txt');
extra.push('native/bridge/registered_buddy.go', 'native/bridge/registered_buddy_test.go', 'tests/RegisteredBuddyCases.cs', 'evidence/native/registered-buddy-checkpoint.json');
for (let i=1;i<=6;i++) extra.push('evidence/native/registered-buddy-case-'+i+'-start.txt');
for (const name of ['registered-buddy-bursts.txt','registered-buddy-passive.txt','registered-buddy-passive-bursts.txt','registered-buddy-next.txt','registered-buddy-passive-next.txt']) extra.push('evidence/native/'+name);
extra.push('ios-client/Assets/Recovery/Runtime/CombatTableSet.cs', 'tests/CombatTableSetTests.cs', 'scripts/test-combat-table-set.mjs', 'evidence/ios-package-search/ios-combat-table-tests.json', '战斗主表接入.md');
extra.push('tests/LiveIosServiceProbe.cs', 'scripts/test-live-ios-service.mjs', 'scripts/inspect-live-ios-ui.mjs', 'scripts/export-unity.mjs', 'evidence/ios-package-search/ios-live-service-test.json', 'evidence/ios-package-search/ios-live-service-uuid-format-failure.json', 'evidence/ios-package-search/ios-live-ui-inspection.json', '公网联调结果.md');
extra.push('scripts/Start-IosClient006CloudBuild.ps1', 'scripts/Get-IosClient006CloudBuild.ps1', 'scripts/Verify-IosClient006IPA.ps1');
extra.push('scripts/Start-IosClient007CloudBuild.ps1', 'scripts/Get-IosClient007CloudBuild.ps1', 'scripts/Verify-IosClient007IPA.ps1');
extra.push('evidence/ios-package-search/sideloadly-anisette-error.json');
extra.push('evidence/ios-package-search/ios-live-character-flow.json', 'evidence/ios-package-search/device-http0-report.json');
extra.push('evidence/ios-package-search/ios-live-quest-limit-failure.json', 'evidence/ios-package-search/ios-live-quest-flow.json');
extra.push('tests/ios-build-structure.test.mjs', 'evidence/ios-structure-tests.json', 'evidence/ios-package-search/device-ats-diagnosis.json');
extra.push('AGENTS.md', 'evidence/ios-package-search/ios-client007-cloud-request.json');
extra.push('详细执行计划.md');
extra.push('evidence/ios-package-search/content-loader-staging-tests.json', 'evidence/ios-package-search/ios-client007-artifact-verification.json', 'evidence/ios-package-search/ios-client007-cloud-status.json', 'evidence/ios-package-search/ipa-client007-delivery.json', 'scripts/Test-ContentLoaderStaging.ps1', 'tests/ContentLoaderStagingTests.cs');
extra.push('tests/RealResourceTransitionTests.cs', 'scripts/test-real-resource-transition.mjs', 'evidence/ios-package-search/ios-real-resource-transition-tests.json');
extra.push('evidence/ios-package-search/device-client007-connectivity.json', 'evidence/ios-package-search/content-loader-rollback-tests.json');
extra.push('scripts/Start-IosClient008CloudBuild.ps1', 'scripts/Get-IosClient008CloudBuild.ps1', 'scripts/Verify-IosClient008IPA.ps1');
extra.push('evidence/ios-package-search/ios-client006-cloud-request.json', 'evidence/ios-package-search/ios-client006-cloud-status.json', 'evidence/ios-package-search/ipa-client006-delivery.json', 'evidence/ios-package-search/ios-client006-cloud-review.json', 'evidence/ios-package-search/ios-client006-artifact-verification.json');
extra.push('native/bridge/registered_sphere.go', 'tests/RegisteredSphereCases.cs', 'evidence/native/registered-sphere-checkpoint.json');
for(let i=1;i<=5;i++)for(const part of ['start','action','remaining','next','next-turn'])extra.push('evidence/native/registered-sphere-case-'+i+'-'+part+'.txt');
extra.push('evidence/native/registered-sphere-delayed-unavailable.txt', 'evidence/native/registered-sphere-delayed-available.txt');
extra.push('evidence/ios-package-search/ios-client005-cloud-request.json', 'evidence/ios-package-search/ios-client005-cloud-status.json', 'evidence/ios-package-search/ipa-client005-delivery.json', 'evidence/ios-package-search/ios-client005-artifact-verification.json');
extra.push('scripts/probe-order03-live-battle-contract.mjs', 'evidence/ios-package-search/order03-live-battle-contract.json', 'evidence/ios-package-search/order03-source-link-reproduction.json');
extra.push('scripts/test-order03-arthur-type.mjs', 'tests/Order03ArthurTypeProbe.cs', 'evidence/ios-package-search/order03-arthur-type.json');
extra.push('scripts/test-order03-enemy-flag.mjs', 'tests/Order03EnemyFlagProbe.cs', 'evidence/ios-package-search/order03-enemy-flag.json', 'evidence/ios-package-search/order03-enemy-flag-before-fix.json');
extra.push('scripts/test-order03-other-selected-cards.mjs', 'tests/Order03OtherSelectedCardsProbe.cs', 'evidence/ios-package-search/order03-other-selected-cards.json', 'evidence/ios-package-search/order03-other-selected-cards-before-fix.json');
extra.push('scripts/test-order03-selected-counts.mjs', 'tests/Order03SelectedCountsProbe.cs', 'evidence/ios-package-search/order03-selected-counts.json', 'evidence/ios-package-search/order03-selected-counts-before-fix.json');
extra.push('scripts/test-order03-selected-tags.mjs', 'tests/Order03SelectedTagsProbe.cs', 'evidence/ios-package-search/order03-selected-tags.json', 'evidence/ios-package-search/order03-selected-tags-before-fix.json');
extra.push('scripts/test-order03-friend-selected.mjs', 'tests/Order03FriendSelectedProbe.cs', 'evidence/ios-package-search/order03-friend-selected.json', 'evidence/ios-package-search/order03-friend-selected-before-fix.json');
extra.push('scripts/test-order03-target-job.mjs', 'tests/Order03TargetJobProbe.cs', 'evidence/ios-package-search/order03-target-job.json', 'evidence/ios-package-search/order03-target-job-before-fix.json');
extra.push('tests/Order03StatusProbe.cs', 'scripts/test-order03-status.mjs', 'evidence/ios-package-search/order03-status.json', 'evidence/ios-package-search/order03-status-before-fix.json');
extra.push('scripts/Collect-Order03RemainingEvidence.ps1', 'scripts/Collect-Order03-Remaining.cmd', 'scripts/test-order03-remaining-collector.mjs', 'scripts/prepare-order03-remaining-collector.mjs', 'evidence/ios-package-search/order03-remaining-collector-tests.json', 'evidence/ios-package-search/order03-remaining-collector-package.json');
extra.push('scripts/test-order03-buff-exec.mjs', 'evidence/ios-package-search/order03-buff-exec.json', 'evidence/ios-package-search/order03-buff-exec-before-fix.json', 'scripts/audit-order03-remaining.mjs', 'evidence/ios-package-search/order03-remaining-validation.json');
extra.push('tests/Order03RawStaticProbe.go', 'scripts/test-order03-raw-static-loader.mjs', 'evidence/ios-package-search/order03-raw-static-loader.json');
extra.push('patches/ios-server-reviewed.patch', 'scripts/reproduce-order03-reviewed-patch.mjs', 'evidence/ios-package-search/order03-reviewed-patch-reproduction.json', 'evidence/ios-package-search/order03-reviewed-source-contract-tests.json');
extra.push('scripts/Collect-Order03RuntimeSelection.ps1', 'scripts/Collect-Order03-Selection.cmd', 'scripts/test-order03-runtime-selection.mjs', 'scripts/prepare-order03-runtime-selection.mjs', 'evidence/ios-package-search/order03-runtime-selection-tests.json', 'evidence/ios-package-search/order03-runtime-selection-package.json');
extra.push('scripts/audit-order03-runtime-selection.mjs', 'evidence/ios-package-search/order03-runtime-selection-validation.json', 'tests/Order03LiveEngine.go', 'scripts/prepare-order03-live-engine.mjs', 'scripts/order03-engine-client.mjs', 'scripts/test-order03-live-engine.mjs', 'scripts/order03-play-rounds.mjs', 'scripts/probe-order03-real-battle.mjs', 'evidence/ios-package-search/order03-real-battle.json', 'evidence/ios-package-search/order03-real-battle-continue.json', 'evidence/ios-package-search/order03-real-battle-continue-attempt1.json');
extra.push('scripts/audit-order03-real-battle.mjs', 'evidence/ios-package-search/order03-real-battle-validation.json');
extra.push('scripts/Verify-Order03Restart.ps1','scripts/Verify-Order03-Restart.cmd','scripts/test-order03-restart-probe.mjs','scripts/prepare-order03-restart-probe.mjs','evidence/ios-package-search/order03-restart-probe-tests.json','evidence/ios-package-search/order03-restart-probe-package.json','evidence/ios-package-search/order03-real-defeat-retry.json','evidence/ios-package-search/order03-real-battle-continue-attempt2.json');
extra.push('scripts/server-verification-names.mjs','scripts/test-server-verification-names.mjs','scripts/prepare-order03-remaining-collector.mjs','scripts/prepare-order03-runtime-selection.mjs','scripts/prepare-order03-server-handoff.mjs','scripts/test-order03-remaining-collector.mjs','scripts/test-order03-runtime-selection.mjs','scripts/Collect-Order03RemainingEvidence.ps1','evidence/ios-package-search/order03-remaining-collector-tests-lyd.json','evidence/ios-package-search/order03-remaining-collector-package-lyd.json','evidence/ios-package-search/order03-runtime-selection-tests-lyd.json','evidence/ios-package-search/order03-runtime-selection-package-lyd.json','evidence/ios-package-search/order03-restart-probe-tests-lyd.json','evidence/ios-package-search/order03-restart-probe-package-lyd.json','evidence/ios-package-search/order03-restart-result-validation-lyd.json');
for (const name of ['victory-input.txt','victory-steps.jsonl','defeat-input.txt','defeat-steps.jsonl']) extra.push('evidence/ios-package-search/order03-real-battle-continue-logs-20261125T012325108Z/'+name);
for (const name of ['victory-input.txt','victory-steps.jsonl']) extra.push('evidence/ios-package-search/order03-real-defeat-retry-logs-20261125T012147232Z/'+name);
for (const name of ['victory-input.txt','victory-steps.jsonl','defeat-input.txt','defeat-steps.jsonl']) extra.push('evidence/ios-package-search/order03-real-battle-continue-logs-20261125T010255139Z/'+name);
extra.push('scripts/audit-order03-restart-result.mjs','scripts/audit-order03-publication-provenance.mjs','evidence/ios-package-search/order03-restart-result-20261125T025522528Z-validation-lyd.json','evidence/ios-package-search/order03-publication-provenance-lyd.json','evidence/ios-package-search/order03-restart-probe-packaged-tests-lyd.json');
extra.push('scripts/audit-order03-published-client-snapshot.mjs','scripts/audit-order03-restart-latest.mjs','scripts/test-order03-solo-persistence-boundary.mjs','scripts/prepare-order03-durable-solo-source.mjs','scripts/test-order03-durable-solo.mjs','scripts/build-order03-durable-solo.mjs','scripts/Install-Order03SoloFix.ps1','scripts/test-order03-solo-fix-installer.mjs','scripts/prepare-order03-solo-fix-package.mjs','tests/Order03SoloPersistenceBoundary_test.go','tests/Order03DurableSolo_test.go','patches/order03-durable-solo-lyd.patch');
for(const name of ['order03-published-client-snapshot-lyd.json','order03-solo-persistence-boundary-lyd.json','order03-restart-result-20261125T031514670Z-validation-lyd.json','order03-durable-solo-source-lyd.json','order03-durable-solo-tests-lyd.json','order03-durable-solo-build-lyd.json','order03-solo-fix-installer-tests-lyd.json','order03-solo-fix-installer-packaged-tests-lyd.json','order03-solo-fix-package-lyd.json'])extra.push('evidence/ios-package-search/'+name);
extra.push('evidence/ios-package-search/order03-solo-fix-install-screenshot-lyd.json');
extra.push('scripts/Sync-Order03StartupPin.ps1','scripts/test-order03-startup-pin.mjs','scripts/prepare-order03-startup-pin-package.mjs','evidence/ios-package-search/order03-startup-pin-tests-lyd.json','evidence/ios-package-search/order03-startup-pin-packaged-tests-lyd.json','evidence/ios-package-search/order03-startup-pin-package-lyd.json','evidence/ios-package-search/order03-startup-blocked-screenshot-lyd.json');
extra.push('scripts/order03-final-receipt-validation.mjs','scripts/audit-order03-final-receipts.mjs','scripts/test-order03-final-receipts.mjs','scripts/audit-order03-completion.mjs','evidence/ios-package-search/order03-final-receipt-validator-tests-lyd.json','evidence/ios-package-search/order03-final-receipts-validation-lyd.json','evidence/ios-package-search/order03-completion-audit-lyd.json');
extra.push('tests/Order05EntryFlowTests.cs','tests/Order05LiveEntryProbe.cs','tests/NormalizeCubismMetadata.cs','tests/CubismModelGeometryProbe.cs');
extra.push('scripts/check-order05-runtime-reference.mjs','scripts/check-unity-runtime-api.mjs','scripts/test-order05-entry-flow.mjs','scripts/probe-order05-live-entry.mjs','scripts/test-order05-cubism-geometry.mjs','scripts/decompile-order05-live2d-reference.mjs','scripts/prepare-order05-cubism-source.mjs','scripts/prepare-order05-cubism-shaders.mjs','scripts/prepare-order05-original-scenes.mjs','scripts/prepare-order05-world-assets.mjs','scripts/Import-Order05-SceneResources-lyd.ps1','scripts/Import-Order05-RoleImages-lyd.ps1','scripts/prepare-order05-scene-resource-collector.mjs','scripts/test-order05-scene-resource-collector.mjs');
for(const name of ['order05-cubism-geometry-differential.json','order05-cubism-reference-runtime-failure.json','order05-cubism-source-preparation.json','order05-dialog-inputs.json','order05-entry-flow-tests.json','order05-live2d-reference-decompilation.json','order05-live-entry-2026-11-25T11-51-01-013Z.json','order05-live-entry-2026-11-25T11-53-36-532Z.json','order05-original-scene-preparation.json','order05-original-scene-runtime-fixes.json','order05-role-cache-analysis-inputs.json','order05-role-image-collector-package-lyd.json','order05-role-image-import-validation-lyd.json','order05-runtime-api-check.json','order05-runtime-api-legacy-compiler-failure.json','order05-runtime-reference-compilation.json','order05-scene-resource-collector-package-lyd.json','order05-scene-resource-collector-tests-lyd.json','order05-scene-resource-import-validation-lyd.json','order05-scene-resource-inputs.json','order05-world-resource-preparation.json'])extra.push('evidence/ios-package-search/'+name);
const trackedResult=spawnSync('git',['-c','safe.directory='+dir.replaceAll('\\','/'),'-C',dir,'ls-files','-z'],{encoding:'utf8',windowsHide:true});
if(trackedResult.status!==0)throw new Error('Could not read the existing audited CI checkpoint: '+trackedResult.stderr);
const tracked=trackedResult.stdout.split('\0').filter(p=>p&&p!=='ci-source-manifest.json');
const paths = new Set([...tracked, ...manifest.files.map(f => f.path), ...extra, 'scripts/review-ci-checkpoint.mjs']);
const actual = [];
function walk(base, relative = '') {
 for (const entry of fs.readdirSync(base, {withFileTypes: true})) {
  if (!relative && (entry.name === '.git' || entry.name === 'test-output')) continue;
  const rel = path.posix.join(relative, entry.name), abs = path.join(base, entry.name);
  if (entry.isSymbolicLink()) throw new Error('Unexpected symlink: ' + rel);
  if (entry.isDirectory()) walk(abs, rel);
  else if (entry.isFile() && rel !== 'ci-source-manifest.json') actual.push(rel);
  else if (!entry.isFile()) throw new Error('Unexpected file type: ' + rel);
 }
}
walk(dir);
if (actual.some(p => !paths.has(p)) || [...paths].some(p => !actual.includes(p))) throw new Error('CI tree differs from explicit manifest and checkpoint list: unexpected=' + JSON.stringify(actual.filter(p => !paths.has(p))) + ', missing=' + JSON.stringify([...paths].filter(p => !actual.includes(p))));
manifest.files = [...paths].sort().map(p => {
 const data = fs.readFileSync(path.join(dir, p));
 return {path: p, bytes: data.length, sha256: crypto.createHash('sha256').update(data).digest('hex')};
});
manifest.bytes = manifest.files.reduce((sum, f) => sum + f.bytes, 0);
manifest.generatedAt = new Date().toISOString();
manifest.privacy = 'Explicit source, synthetic fixtures, tests and acceptance evidence only; original DLLs, user account data, saves, credentials, licenses and local tools are excluded.';
manifest.execution = 'Source checkpoint; the Windows comparisons require a separately supplied original DLL. This manifest does not claim an iOS build or device validation for this source.';
fs.writeFileSync(file, JSON.stringify(manifest, null, 2) + '\n');
console.log(JSON.stringify({files: manifest.files.length, bytes: manifest.bytes, gameplayReady: manifest.gameplayReady}, null, 2));
