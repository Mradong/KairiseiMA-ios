import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import {lydFilename} from './server-verification-names.mjs';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const base = fs.mkdtempSync(path.join(root, 'test-output/order03-remaining-collector-'));
const script = process.env.ORDER03_TEST_SCRIPT || path.join(root, 'scripts/Collect-Order03RemainingEvidence.ps1');
const expectedPath = path.join(root, 'evidence/ios-package-search/order03-expected-tables.json');
const sha = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const write = (name, data) => {
  const filename = path.join(base, name);
  fs.mkdirSync(path.dirname(filename), { recursive: true });
  fs.writeFileSync(filename, data);
  return filename;
};
const variableString = text => {
  const content = Buffer.from(text);
  const prefix = [];
  let length = content.length;
  do {
    let byte = length & 127;
    length = Math.floor(length / 128);
    if (length) byte |= 128;
    prefix.push(byte);
  } while (length);
  return Buffer.concat([Buffer.from(prefix), content]);
};
const header = Buffer.alloc(32);
Buffer.from('\xff Go buildinf:', 'latin1').copy(header);
header[14] = 8;
header[15] = 2;
write('bin/kairi-server.exe', Buffer.concat([
  Buffer.alloc(64), header, variableString('go1.25.1'),
  variableString('0'.repeat(16) + 'path\tkairisei.local/server/cmd/kairi-server\nmod\tkairisei.local/server\t(devel)\nbuild\tGOOS=windows\nbuild\tGOARCH=amd64\nbuild\tvcs.revision=' + '1'.repeat(40) + '\nbuild\t-ldflags="-s -w -X synthetic.secret=should-not-be-exported"\n' + '0'.repeat(16))
]));
write('isolated-config.json', JSON.stringify({ original_server_root: base, secret: 'SYNTHETIC-CONFIG-MUST-STAY-PRIVATE' }));
write('resource-set/resource-set.json', JSON.stringify({ entrypoints: {
  'cn-combat-card-master': '../masters/cards',
  'cn-combat-master-root': '../masters/combat',
  'cn-battle-master': '../masters/static/battle.json'
} }));
for (const table of JSON.parse(fs.readFileSync(expectedPath)).tables) {
  const directory = ['card.csv', 'buddy.csv', 'card_alchemist.csv'].includes(table.name) ? 'cards' : 'combat';
  write('masters/' + directory + '/' + table.name, '1,SYNTHETIC\n');
}
write('masters/static/battle.json', JSON.stringify({ replays: [{ bossid: 30010102, waves: [{ enemy_partyid: 30010102 }] }] }));
write('masters/static/cn602-normal-quest-runtime.json', JSON.stringify({ replays: [] }));
write('source/server/go.mod', 'module kairisei.local/server\n\ngo 1.25.0\n');
write('source/server/cmd/kairi-server/main.go', 'package main\nfunc main() {}\n');
write('source/server/_local/data/private.go', 'SYNTHETIC-ACCOUNT-DATA-MUST-STAY-PRIVATE');
write('Order03-Provided-Evidence/contracts.json', JSON.stringify({ tests: [{ path: '/TeamBattleSoloContinue', state: 'NOT_TESTED' }] }));
write('Order03-Provided-Evidence/private.json', JSON.stringify({ sess_key: 'SYNTHETIC-SESSION-MUST-STAY-PRIVATE' }));
write('Start-IOS-Test.ps1', '# Synthetic startup script\nWrite-Host "fixture"\n');
write('_local/runtime/test/stdout.log', [
  JSON.stringify({ time: '2026-11-24T01:00:00Z', msg: 'validated CN runtime content gate', team_battle_boss_entries: 5534 }),
  JSON.stringify({ time: '2026-11-24T01:00:01Z', msg: 'request', sess_key: 'SYNTHETIC-LOG-SECRET-MUST-STAY-PRIVATE' })
].join('\n'));
const originalFiles = ['isolated-config.json','bin/kairi-server.exe','masters/static/battle.json','source/server/cmd/kairi-server/main.go'];
const originals = originalFiles.map(name => [name, sha(path.join(base, name))]);
const run = spawnSync('powershell.exe', ['-NoProfile','-ExecutionPolicy','Bypass','-File',script,
  '-TestRoot',base,'-Port','65533','-ExpectedTables',expectedPath], { encoding: 'utf8', windowsHide: true, timeout: 120000 });
assert.equal(run.status, 0, run.stdout + run.stderr);
const output = path.join(base, fs.readdirSync(base).find(name => name.startsWith('Order03-Remaining-') && !name.endsWith('.zip')));
const load = name => JSON.parse(fs.readFileSync(path.join(output, lydFilename(name)), 'utf8'));
const runtime = load('runtime.json');
assert.equal(runtime.goBuildInfo.goVersion, 'go1.25.1');
assert.equal(runtime.goBuildInfo.settings['vcs.revision'], '1'.repeat(40));
assert.equal(runtime.goBuildInfo.settings.ldflagsStripSymbols, true);
assert.equal(runtime.goBuildInfo.settings.ldflagsStripDwarf, true);
assert.equal(runtime.goBuildInfo.settings.ldflagsHasXOverride, true);
assert.equal(runtime.tables.length, 29);
assert.equal(runtime.tables.filter(table => table.matchesReceived).length, 0);
assert.ok(runtime.effectiveLoadedBytes.startsWith('UNKNOWN'));
assert.equal(load('source.json').files.length, 2);
assert.equal(load('source.json').commit, 'UNKNOWN');
assert.ok(fs.existsSync(path.join(output, 'static/battle-lyd.json')));
assert.ok(fs.existsSync(path.join(output, 'static/normal_quest-lyd.json')));
assert.ok(fs.existsSync(path.join(output, 'provided/contracts-lyd.json')));
assert.equal(fs.existsSync(path.join(output, 'provided/private.json')), false);
assert.equal(fs.existsSync(path.join(output, 'source/_local/data/private.go')), false);
for (const [name, hash] of originals) assert.equal(sha(path.join(base, name)), hash);
const serialized = JSON.stringify([runtime, load('source.json'), load('capture.json')]);
assert.equal(serialized.includes('SYNTHETIC-CONFIG-MUST-STAY-PRIVATE'), false);
assert.equal(serialized.includes('should-not-be-exported'), false);
assert.ok(fs.existsSync(output + '.zip'));
assert.equal(load('startup-observations.json')[0].records[0].fields.team_battle_boss_entries, 5534);
assert.equal(JSON.stringify(load('startup-observations.json')).includes('SYNTHETIC-LOG-SECRET'), false);
const report = { checkedAt: new Date().toISOString(), success: true, platform: 'Windows PowerShell 5.1',
  syntheticFixture: true, scriptSha256: sha(script), checks: [
    'embedded Go build metadata without executing binary or installing Go',
    '29 separate-root table identities', 'resource-set relative entrypoints',
    'byte-exact raw static JSON capture', 'source discovery without Git metadata',
    'private config and account directories excluded', 'session-bearing supplied JSON omitted',
    'ldflags value kept private', 'input hashes unchanged', 'unique directory and ZIP generated',
    'loaded-content and unexecuted contracts remain UNKNOWN/NOT_TESTED'
  ], output, scope: 'Synthetic Windows desktop validation only. Actual server execution and actual deployment/source link remain unverified.' };
fs.writeFileSync(path.join(root, 'evidence/ios-package-search/order03-remaining-collector-tests-lyd.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify(report, null, 2));
