import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const read=file=>fs.readFileSync(path.join(root,file));
const sha=file=>crypto.createHash('sha256').update(read(file)).digest('hex');
const load=file=>JSON.parse(read(file).toString('utf8').replace(/^\uFEFF/,''));
const p='evidence/ios-package-search/';
const planRow=read('详细执行计划.md').toString('utf8').split(/\r?\n/).find(line=>/^\| 03 \|/.test(line));assert(planRow);
const readiness=load(p+'order03-readiness.json');assert(readiness.ready&&readiness.gates.every(gate=>gate.state==='verified'));
for(const source of readiness.evidenceHashes)assert.equal(sha(source.file),source.sha256);
const publication=load(p+'order03-publication-provenance-lyd.json');
const publishedSnapshot=load(p+'order03-published-client-snapshot-lyd.json');
const snapshot=load(publishedSnapshot.snapshot.file),metadata=load(p+'order03-modern-metadata-enums.json');
assert.equal(sha(publishedSnapshot.snapshot.file),publishedSnapshot.snapshot.sha256);
assert.equal(publication.publication.tag,'2.0.4');assert.equal(publication.publication.baseCommit,'d1a202eb539b99511e8a33f3975fac409ab545bf');
const required=read('ios-client/Assets/Recovery/Runtime/CombatTableSet.cs').toString('utf8').match(/RequiredTables = \{([\s\S]*?)\};/)[1].match(/[a-z_]+\.csv/g);
assert.equal(required.length,20);assert.equal(snapshot.tables.length,29);
assert.deepEqual(required.filter(name=>!snapshot.tables.some(table=>table.name===name)),[]);
for(const table of snapshot.tables) {
  const expected=publication.tables.find(file=>file.name===table.name);assert(expected);
  const bytes=Buffer.from(table.text,'utf8');assert.equal(bytes.length,expected.bytes);
  assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'),expected.sha256);
  assert.equal(sha(expected.receivedFile),expected.sha256);
}
assert.equal(snapshot.enums.length,23);
assert.equal(snapshot.enums.reduce((sum,family)=>sum+family.values.length,0),822);
for(const family of snapshot.enums)assert.deepEqual(family.values,metadata.families.find(f=>f.name===family.name).values.map(value=>({name:value.name,value:value.value})));
assert.equal(sha(publishedSnapshot.metadata.file),metadata.source.metadataSha256);
const referencesFile='inputs/server-order03/Order03-Combat-Verified-20261123/audit/reference-summary.json';
const references=load(referencesFile);
assert.equal(Object.values(references.checks).reduce((sum,check)=>sum+check.edges,0),446725);
assert.equal(Object.values(references.checks).reduce((sum,check)=>sum+check.missing_edges,0),0);
const delivery=load(p+'order03-server-delivery-validation.json');assert.equal(delivery.reproducedOfflineAudit.missingReferences,0);
const reviewed=load(p+'order03-reviewed-patch-reproduction.json'),build=load(p+'order03-durable-solo-build-lyd.json');
assert(reviewed.build.exactObservedBinaryReproduction&&reviewed.allBaseAndPatchedGitBlobIdentitiesMatched);
assert.equal(sha(reviewed.inputPatch.file),reviewed.inputPatch.sha256);
assert.equal(sha(build.patch.file),build.patch.sha256);assert.equal(sha(build.build.executable),build.build.sha256);
assert.equal(build.base.observedExecutableSha256,reviewed.build.sha256);
const battle=load(p+'order03-real-battle-validation.json');
assert(battle.success&&battle.realServiceVictoryAccepted&&battle.victoryRetryMatched&&battle.paidContinueAccepted&&battle.continueRetryMatched&&battle.realDefeatAccepted);
assert.equal(battle.continueDebit,50);assert.equal(battle.defeatFrozenResultRetry,'VERIFIED');
for(const log of battle.logs)assert.equal(sha(log.file),log.sha256);
const receipts=load(p+'order03-final-receipts-validation-lyd.json');
for(const field of ['analysisCompleted','installationReceiptVerified','startupPinReceiptVerified','reportedProcessRestartVerified','reportedSameOpeningVerified','singleEntryBPDeltaVerified','nonClearAbandonVerified','receivedRestartChecksumVerified'])assert.equal(receipts[field],true);
for(const file of [...receipts.inputFiles,...receipts.sourceEvidence])assert.equal(sha(file.file),file.sha256);
const handoff='顺序03服务端交付请求.md';assert(fs.statSync(path.join(root,handoff)).size>0);
const archiveDir='inputs/server-order03/Order03-Accepted-Snapshot-2.0.4-lyd';fs.mkdirSync(path.join(root,archiveDir),{recursive:true});
const acceptedSnapshot=archiveDir+'/CombatTables-lyd.json';
fs.copyFileSync(path.join(root,publishedSnapshot.snapshot.file),path.join(root,acceptedSnapshot));
assert.equal(sha(acceptedSnapshot),publishedSnapshot.snapshot.sha256);
const report={checkedAt:new Date().toISOString(),planItem:'03',completionProven:true,reviewer:'root',planRequirement:planRow,
  requirements:[
    {id:'inventory_and_real_snapshot',result:'verified',evidence:[p+'order03-server-delivery-validation.json',p+'order03-published-client-snapshot-lyd.json'],observations:'20 required plus 9 extensions; exact received CSV text and 23/822 enum values; physical snapshot byte identities checked.'},
    {id:'traceable_source_version_hash',result:'verified_with_scope',evidence:[p+'order03-publication-provenance-lyd.json',p+'order03-published-client-snapshot-lyd.json'],observations:'Public reconstruction 2.0.4 release/tag/base commit, server table manifest, exact public client metadata, runtime resource manifest and byte hashes match. Official original extraction is explicitly unknown.'},
    {id:'reference_integrity_and_missing_items',result:'verified_with_scope',evidence:[p+'order03-server-delivery-validation.json',referencesFile,p+'order03-modern-defaults.json'],observations:'23 mapped reference groups, 446725 positive edges, zero missing; 25 VALUE and 36 AI shape observations are retained with stated scope. This is not an assertion about every symbolic/gameplay parameter.'},
    {id:'executable_and_runtime_link',result:'verified_with_scope',evidence:[p+'order03-reviewed-patch-reproduction.json',p+'order03-runtime-selection-validation.json',p+'order03-durable-solo-build-lyd.json',p+'order03-final-receipts-validation-lyd.json'],observations:'Pinned base plus reviewed iOS patch reproduces old observed binary; reviewed three-file solo/maintenance patch compiles new binary. Received install and live process identities match the new build.'},
    {id:'server_request_reply_and_redacted_contract',result:'verified_with_scope',evidence:[handoff,p+'order03-live-battle-contract.json',p+'order03-real-battle-validation.json',p+'order03-final-receipts-validation-lyd.json'],observations:'Versioned requirements and server-provided tables/source/receipts retained. Real isolated-service battle and continuation evidence plus actual new-process opening recovery and one 15-BP debit verified in recorded scopes.'}
  ],
  acceptedSnapshot:{file:acceptedSnapshot,bytes:read(acceptedSnapshot).length,sha256:sha(acceptedSnapshot),projectVersion:'2.0.4',tables:29,enums:23,enumConstants:822,productionClientAssetDeployed:false},
  retainedMissingOrUnverified:[
    'Official original-client extraction input, region/version, tool/date and transformations are UNKNOWN; project publication is proved, official extraction is not.',
    'Full release ZIP digests were not checked; downloaded ZIP manifest/metadata members and received table hashes were checked.',
    'Reference completeness covers the explicitly mapped positive-ID fields. Full skill/AI semantics and all original behaviors remain for later plan rows.',
    'Battle victory/paid continuation evidence predates the durable-solo patch; source change scope and targeted persistence/maintenance checks are recorded, not a claim of repeated full gameplay on every version.',
    'This is source/data and desktop/isolated-service acceptance. iOS AssetBundle delivery, ARM64, phone mid-battle state and whole-game/device acceptance remain separate.'
  ],
  scope:'Completes the exact plan item 03 data/provenance/contract acceptance with explicit missing items. Does not complete RESOURCE-01/BATTLE-01 as whole-game items or certify the full iOS client.',
  sourceFiles:[...new Set([p+'order03-readiness.json',...readiness.evidenceHashes.map(file=>file.file),referencesFile,handoff])].map(file=>({file,sha256:sha(file)})),
  scriptSha256:sha('scripts/audit-order03-completion.mjs')};
fs.writeFileSync(path.join(root,p+'order03-completion-audit-lyd.json'),JSON.stringify(report,null,2)+'\n');
fs.writeFileSync(path.join(root,archiveDir,'snapshot-manifest-lyd.json'),JSON.stringify(report.acceptedSnapshot,null,2)+'\n');
console.log(JSON.stringify({planItem:'03',completionProven:true,requirements:report.requirements.map(r=>({id:r.id,result:r.result})),acceptedSnapshot:report.acceptedSnapshot,wholeGameComplete:false},null,2));
