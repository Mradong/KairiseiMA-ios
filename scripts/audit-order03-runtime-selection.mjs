import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const input = 'inputs/server-order03/Order03-Selection-20261124T092532030Z-9ff1e0';
const read = filename => fs.readFileSync(path.join(root, filename));
const sha = filename => crypto.createHash('sha256').update(read(filename)).digest('hex');
const load = filename => JSON.parse(read(filename).toString('utf8').replace(/^\uFEFF/, ''));
const selection = load(input + '/selection.json');
const reproduction = load('evidence/ios-package-search/order03-reviewed-patch-reproduction.json');
const expected = load('evidence/ios-package-search/order03-expected-tables.json');
const previous = load('inputs/server-order03/Order03-Remaining-20261124T074548237Z-2260ff/runtime.json');
assert.equal(sha(input + '.zip'), '71f81476ceb6fa2d239f38e80372a75549dfe678725ae5bbef0ee4d667bc2990');
const checksum = /^([a-f0-9]{64})  selection\.json\s*$/.exec(read(input + '/SHA256SUMS.txt').toString('utf8'));
assert(checksum);
assert.equal(checksum[1], sha(input + '/selection.json'));
assert.equal(selection.schemaVersion, 1);
assert.equal(selection.collector, 'order03-selection-20261124-1');
assert.equal(selection.state, 'selected_paths_hashed');
assert.equal(selection.process.port, 27040);
assert.equal(selection.process.executable.sha256, reproduction.build.sha256);
assert.equal(selection.process.executable.bytes, reproduction.build.bytes);
assert.equal(selection.expectedExecutableSha256, reproduction.build.sha256);
assert.equal(selection.process.startUtc, previous.processBefore.listeners[0].startUtc);
assert.equal(selection.sameObservedProcess, true);
assert.equal(selection.executableStableAfterCapture, true);
assert.deepEqual(selection.selectionCounts, { resourceSet:1, platformProfiles:1, platformConfig:0 });
assert.equal(selection.selectedResources.manifest.sha256, previous.selectedResourceManifestIdentity.sha256);
assert.equal(selection.selectedResources.manifestStableAfterRead, true);
const processStart = Date.parse(selection.process.startUtc);
assert(Number.isFinite(processStart) && processStart < Date.parse(selection.collectedUtc));
const identity = value => {
  assert(Number.isSafeInteger(value.bytes) && value.bytes > 0);
  assert.match(value.sha256, /^[0-9a-f]{64}$/);
  assert.equal(value.stable, true);
  assert(Date.parse(value.lastWriteUtc) <= processStart, 'Selected file reports a post-start modification');
};
identity(selection.process.executable);
identity(selection.selectedResources.manifest);
assert.equal(selection.selectedResources.tables.length, expected.tables.length);
assert.equal(new Set(selection.selectedResources.tables.map(t => t.name)).size, 29);
for (const table of selection.selectedResources.tables) {
  const old = expected.tables.find(t => t.name === table.name);
  assert(old && table.matchesReceived);
  identity(table.identity);
  assert.equal(table.identity.sha256, old.sha256);
  assert.equal(table.identity.bytes, old.bytes);
}
for (const file of selection.selectedResources.staticResources) {
  const old = previous.staticResources.find(value => value.role === file.role);
  assert(old);
  identity(file.identity);
  assert.equal(file.identity.sha256, old.identity.sha256);
}
assert.equal(selection.platformProfiles.length, 1);
const profile = selection.platformProfiles[0];
identity(profile.identity);
assert.equal(profile.platform, 'iOS');
assert.equal(profile.unityVersion, '6000.3.26f1');
assert.equal(profile.contentId, '55c68a8971aafc9a137f79f16d5c2795bfb37c89b7f64bdca1542fc0eb1fd2bb');
for (const value of Object.values(selection.sideEffects)) assert.equal(value, 0);
// This command source is unchanged by the reviewed patch. It loads the
// selected manifest/catalog before opening the HTTP listener.
const commandSource = 'source-review/kairisei-ma-ch/server/cmd/kairi-server/main.go';
const source = read(commandSource).toString('utf8');
const manifestLoad = source.indexOf('cnbootstrap.LoadRuntimeInputs(opts.resourceSet)');
const catalogLoad = source.indexOf('multiplayer.LoadCombatCatalog(inputs.CombatCardMaster, inputs.CombatMasterRoot)');
const listener = source.indexOf('net.Listen("tcp", server.Addr)');
assert(manifestLoad > 0 && catalogLoad > manifestLoad && listener > catalogLoad);
const report = {
  checkedAt:new Date().toISOString(), success:true, planItem:'03',
  receivedArchive:{ file:input+'.zip', bytes:read(input+'.zip').length, sha256:sha(input+'.zip'), entries:2, checksumVerified:true },
  inputFile:{ file:input+'/selection.json', sha256:sha(input+'/selection.json') },
  reproducedExecutableSha256:reproduction.build.sha256,
  processStartUtc:selection.process.startUtc, collectionUtc:selection.collectedUtc,
  selectedRuntimeInputsVerified:true,
  selectedManifest:selection.selectedResources.manifest,
  matchedTables:29, selectedStaticResources:selection.selectedResources.staticResources,
  selectedPlatform:profile,
  declaredFileTimesPrecedeObservedProcessStart:true,
  sourceLoadOrder:{ manifestBeforeCatalog:true, catalogBeforeListener:true, sourceFile:commandSource, sourceSha256:sha(commandSource), baseCommit:reproduction.baseCommit },
  scriptSha256:sha('scripts/audit-order03-runtime-selection.mjs'),
  scope:'User-supplied process-argument observation for the exactly reproduced executable, selected file identities matching received tables/static data, and verified source startup load order. These establish the selected startup resource cohort within this evidence scope. Process memory bytes and mutable publication/account state were not captured. Historical CSV extraction and real battle contracts remain unverified.'
};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/order03-runtime-selection-validation.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report,null,2));
