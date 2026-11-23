import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const out = path.join(root, 'test-output/order03-enemy-variable');
fs.mkdirSync(out, {recursive: true});
const sha = p => crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const original = path.join(root, 'inputs/native/kairisei_battlecore.dll');
const rebuilt = path.join(root, 'native/build/windows-amd64/kairisei_battlecore_rebuilt.dll');
const source = path.join(root, 'tests/Order03EnemyVariableProbe.cs');
const fixture = path.join(root, 'tests/fixtures/native-session/tables.json');
const expected = JSON.parse(fs.readFileSync(path.join(root, 'evidence/native/go-symbols-summary.json'), 'utf8')).sha256;
if (sha(original) !== expected) throw Error('Original DLL hash differs');
const json = path.join(root, 'tools/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll');
fs.copyFileSync(json, path.join(out, 'Newtonsoft.Json.dll'));
const exe = path.join(out, 'probe.exe'), raw = path.join(out, 'results.json');
const build = spawnSync(path.join(process.env.WINDIR, 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'),
  ['/nologo', '/platform:x64', '/out:' + exe, '/reference:' + json, source], {encoding: 'utf8', windowsHide: true});
if (build.status !== 0) throw Error(build.stdout + build.stderr);
const run = spawnSync(exe, [original, rebuilt, fixture, raw], {encoding: 'utf8', windowsHide: true, timeout: 120000});
if (run.status !== 0) throw Error(run.stdout + run.stderr);
const cases = JSON.parse(fs.readFileSync(raw, 'utf8'));
let matched = 0, selected = 0, fallback = 0;
for (const c of cases) {
  if (c.original.createStatus !== 0 || c.original.turns.length !== 3) throw Error('Original scenario did not execute');
  let counter = 0;
  for (const commands of c.original.turns) {
    const value = c.index >= 0 && c.index < 5 ? counter : -1;
    const branch = c.ordinal === -1 || (BigInt(c.lower) <= BigInt(value) && BigInt(value) <= BigInt(c.upper));
    const damage = branch ? 13 : 7;
    const hits = commands.filter(x => x.startsWith('60,'));
    if (hits.length !== 1 || hits[0].split(',').slice(0, 4).join(',') !== `60,5,1,-${damage}`)
      throw Error('Original branch marker differs: ' + JSON.stringify({c: {...c, original: undefined, rebuilt: undefined}, commands, value, branch}));
    const variableCommands = commands.filter(x => x.startsWith('94,'));
    if (c.index >= 0 && c.index < 5) {
      counter = (counter + c.delta) | 0;
      const values = [0, 0, 0, 0, 0]; values[c.index] = counter;
      if (variableCommands.length !== 1 || variableCommands[0].replace(/,$/, '') !== `94,5,${values.join(',')}`)
        throw Error('Original variable output differs: ' + JSON.stringify(variableCommands));
    } else if (variableCommands.length !== 0) throw Error('Invalid index changed a variable');
    if (branch) selected++; else fallback++;
  }
  if (JSON.stringify(c.original) === JSON.stringify(c.rebuilt)) matched++;
}
const observe = process.argv.includes('--observe');
const report = {
  checkedAt: new Date().toISOString(), success: cases.length === 96 && matched === cases.length && selected > 0 && fallback > 0 && sha(original) === expected,
  cases: cases.length, turns: selected + fallback, selected, fallback, fullTraceAndJournalMatches: matched,
  originalSha256: sha(original), rebuiltSha256: sha(rebuilt), probeSha256: sha(source), scriptSha256: sha(fileURLToPath(import.meta.url)), fixtureSha256: sha(fixture),
  bridgeSourceSha256: sha(path.join(root, 'native/bridge/skill_query.go')),
  rawResultsSha256: sha(raw), results: cases.map(c => {
    const compact = x => ({createStatus: x.createStatus, turns: x.turns,
      traceSha256: crypto.createHash('sha256').update(JSON.stringify(x.trace)).digest('hex'),
      journalSha256: x.journal === undefined ? null : crypto.createHash('sha256').update(x.journal).digest('hex')});
    return {...c, original: compact(c.original), rebuilt: compact(c.rebuilt)};
  }),
  scope: 'Windows x64 synthetic registered enemy action, three turns, both branch-condition slots; explicit signed64 bounds, signed32 variable addition/overflow and invalid indices. Supplied schemas label SELF_ENEMY_AI_VAR at 110, 111 or arbitrary test ordinal 900; ordinal -1 is an unconditional control. Full result traces and input journals compared and hashed; raw output remains in test-output. These schema-driven observations do not identify the target enum/version, prove raw empty-CSV default conversion, real tables, complete enemy programs, Unity, ARM64, live admission or device behavior.'
};
const name = 'order03-enemy-variable' + (observe ? '-before-fix' : '') + '.json';
fs.writeFileSync(path.join(root, 'evidence/ios-package-search', name), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({...report, results: undefined}, null, 2));
if (!observe && !report.success) process.exitCode = 1;
