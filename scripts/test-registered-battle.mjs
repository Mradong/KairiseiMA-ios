import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const original = path.join(root, 'inputs/native/kairisei_battlecore.dll');
const rebuilt = path.join(root, 'native/build/windows-amd64/kairisei_battlecore_rebuilt.dll');
const hash = p => crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const expected = JSON.parse(fs.readFileSync(path.join(root, 'evidence/native/go-symbols-summary.json'), 'utf8')).sha256;
if (hash(original) !== expected) throw new Error('Original DLL hash differs');
const exe = path.join(root, 'test-output/native-session-differential/RegisteredBattleDifferential.exe');
fs.mkdirSync(path.dirname(exe), {recursive: true});
const csc = path.join(process.env.WINDIR, 'Microsoft.NET/Framework64/v4.0.30319/csc.exe');
const compile = spawnSync(csc, ['/nologo', '/platform:x64', '/out:' + exe, path.join(root, 'tests/RegisteredBattleDifferential.cs')], {encoding: 'utf8'});
if (compile.status !== 0) throw new Error(compile.stdout + compile.stderr);
const run = spawnSync(exe, [original, rebuilt, path.join(root, 'tests/fixtures/native-session/tables.json'), path.join(root, 'test-output/native-session-differential')], {encoding: 'utf8', timeout: 120000});
const record = {checkedAt: new Date().toISOString(), success: run.status === 0,
 counts: run.status === 0 ? JSON.parse(run.stdout.trim().replace(/^\uFEFF/, '')) : null,
 stderr: run.stderr.trim(), original: {sha256: expected, unchangedAfterTest: hash(original) === expected},
 rebuilt: {sha256: hash(rebuilt)},
 scope: 'Windows x64 synthetic registered mode-1 party: compare creation, 15 battle steps, start/resume CSV, registered-enemy scaled HP and 18 cumulative input-journal snapshots with short/null/repeated reads and failure handling. Other modes, NPCs, nonempty spheres/buddies, enemy programs, burst journals and wave transition are unverified or incomplete. No real resources, account or iOS test.'};
fs.writeFileSync(path.join(root, 'evidence/native/registered-battle-differential-tests.json'), JSON.stringify(record, null, 2) + '\n');
console.log(JSON.stringify(record, null, 2));
if (!record.success || !record.original.unchangedAfterTest) process.exitCode = 1;
