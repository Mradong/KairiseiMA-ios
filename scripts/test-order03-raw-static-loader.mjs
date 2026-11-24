import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const input = path.join(root, 'inputs/server-order03/Order03-Remaining-20261124T074548237Z-2260ff/static');
const source = path.join(root, 'tests/Order03RawStaticProbe.go');
const output = path.join(root, 'test-output/order03-raw-static-loader');
const reviewed = path.join(root, 'source-review/kairisei-ma-ch/server');
const sha = filename => crypto.createHash('sha256').update(fs.readFileSync(filename)).digest('hex');
const battle = path.join(input, 'battle.json');
const normal = path.join(input, 'normal_quest.json');
assert.equal(sha(battle), 'faeac09193b14619b4d4bef4145648dde38c9643f4b9ad663c98fcd39ecf80eb');
assert.equal(sha(normal), '689df69e44ff14936b5def018ab46172a719e1f421a72ff9e86b365d97e90286');
fs.mkdirSync(output, { recursive: true });
fs.copyFileSync(source, path.join(output, 'main.go'));
const module = 'module kairisei.local/server/order03rawstaticprobe\n\ngo 1.25.0\n\nrequire kairisei.local/server v0.0.0\n\nreplace kairisei.local/server => ' + JSON.stringify(reviewed.replaceAll('\\', '/')) + '\n';
fs.writeFileSync(path.join(output, 'go.mod'), module);
const environment = {
  ...process.env,
  GOROOT: path.join(root, 'tools/go'),
  GOPATH: path.join(root, 'tools/go-work'),
  GOMODCACHE: path.join(root, 'tools/go-module-cache'),
  GOCACHE: path.join(root, 'tools/go-build-cache'),
  GOTOOLCHAIN: 'local',
  CGO_ENABLED: '0'
};
const result = spawnSync(path.join(root, 'tools/go/bin/go.exe'), ['run','-mod=mod','.',battle,normal], {
  cwd: output, env: environment, encoding: 'utf8', windowsHide: true, timeout: 120000
});
fs.writeFileSync(path.join(output, 'loader.log'), result.stdout + result.stderr);
if (result.status !== 0) throw new Error(result.stdout + result.stderr);
const loaded = JSON.parse(result.stdout);
assert.equal(loaded.success, true);
assert.equal(loaded.battle.replays, 3102);
assert.equal(loaded.normalQuest.replays, 50);
const raw = JSON.parse(fs.readFileSync(battle, 'utf8'));
const declaredMappings = new Map(raw.own_deck_variants.map(v => [v.source_boss_id, v.boss_id]));
const standaloneRewards = new Map(raw.rewards.map(r => [r.boss_id, r.stage_quest_area_id === 0 && (r.tower_id ?? 0) === 0]));
const expectedMappings = new Map(declaredMappings);
const expectedOptional = [];
for (const group of raw.groups) for (const quest of group['10']) {
  const id = quest['0'];
  if (expectedMappings.has(id) || !standaloneRewards.get(id) || id <= 0 || id >= 100000000 || quest['1'] !== 0 || quest['24'] !== 0) continue;
  expectedMappings.set(id, id + 100000000);
  expectedOptional.push(id + 100000000);
}
assert.equal(declaredMappings.size, 1043);
assert.equal(loaded.battle.ownDeckVariants, expectedMappings.size);
assert.equal(loaded.battle.optionalOwnDeckVariants, expectedOptional.length);
assert.deepEqual(loaded.optionalOwnDeckBossIDs, expectedOptional);
assert.deepEqual(Object.keys(loaded.disabledBossIDs).map(Number).sort((a, b) => a - b), [...expectedOptional].sort((a, b) => a - b));
assert(Object.values(loaded.disabledBossIDs).every(disabled => disabled === true));
assert.deepEqual(loaded.ownDeckMappings, [...expectedMappings].map(([source_boss_id, boss_id]) => ({ source_boss_id, boss_id })));
assert.equal(loaded.projectedEntryBossCount, raw.groups.reduce((count, group) => count + group['10'].length, 0) + expectedMappings.size);
assert.equal(sha(battle), 'faeac09193b14619b4d4bef4145648dde38c9643f4b9ad663c98fcd39ecf80eb');
assert.equal(sha(normal), '689df69e44ff14936b5def018ab46172a719e1f421a72ff9e86b365d97e90286');
const report = {
  checkedAt: new Date().toISOString(), success: true, platform: 'Windows amd64, local Go 1.25.1',
  loaded, inputDeclaredOwnDeckVariants: declaredMappings.size,
  derivedOptionalOwnDeckVariants: expectedOptional.length,
  allLoadedOwnDeckMappingsIndependentlyReproduced: true,
  battleSha256: sha(battle), normalQuestSha256: sha(normal),
  probeSha256: sha(source), scriptSha256: sha(fileURLToPath(import.meta.url)),
  loaderSources: ['battle_master.go','normal_quest_master.go','battle_own_deck.go'].map(name => ({
    file: 'source-review/kairisei-ma-ch/server/internal/masterdata/' + name,
    sha256: sha(path.join(reviewed, 'internal/masterdata', name))
  })),
  scope: loaded.scope
};
const referenceRoot = path.dirname(reviewed);
const revision = spawnSync('git', ['-c', 'safe.directory=' + referenceRoot.replaceAll('\\', '/'), '-C', referenceRoot, 'rev-parse', 'HEAD'], { encoding: 'utf8', windowsHide: true });
if (revision.status !== 0 || !/^[0-9a-f]{40}\s*$/.test(revision.stdout)) throw new Error('Pinned reference commit could not be read');
report.referenceCommit = revision.stdout.trim();
fs.writeFileSync(path.join(root, 'evidence/ios-package-search/order03-raw-static-loader.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({ ...report, loaded: { ...report.loaded, ownDeckMappings: report.loaded.ownDeckMappings.length, optionalOwnDeckBossIDs: report.loaded.optionalOwnDeckBossIDs.length, disabledBossIDs: Object.keys(report.loaded.disabledBossIDs).length } }, null, 2));
