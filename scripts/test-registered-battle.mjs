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
const json=path.join(root,'tools/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll');
fs.copyFileSync(json,path.join(path.dirname(exe),'Newtonsoft.Json.dll'));
const compile = spawnSync(csc, ['/nologo', '/platform:x64', '/reference:'+json, '/out:' + exe,
 path.join(root, 'tests/RegisteredBattleDifferential.cs'), path.join(root,'tests/RegisteredWaveCases.cs'),path.join(root,'tests/RegisteredManagedCases.cs'),
 path.join(root,'ios-client/Assets/Recovery/Runtime/NativeRegisteredBattle.cs')], {encoding: 'utf8'});
if (compile.status !== 0) throw new Error(compile.stdout + compile.stderr);
const run = spawnSync(exe, [original, rebuilt, path.join(root, 'tests/fixtures/native-session/tables.json'), path.join(root, 'test-output/native-session-differential')], {encoding: 'utf8', timeout: 120000});
const record = {checkedAt: new Date().toISOString(), success: run.status === 0,
 counts: run.status === 0 ? JSON.parse(run.stdout.trim().replace(/^\uFEFF/, '')) : null,
 stderr: run.stderr.trim(), original: {sha256: expected, unchangedAfterTest: hash(original) === expected},
 rebuilt: {sha256: hash(rebuilt)},
 scope: 'Windows x64 synthetic registered mode-1 party: original-DLL status, full result CSV and cumulative journal comparisons, actual damage, first-wave victory, registered next wave and second-wave victory. All 35 registered enemy skill slots with zero skill ID are tested through an enemy turn. Twelve cases cover 1/3/5 contiguous packed effects and incoming enemy types 0/1/2/3, changed enemy HP, failure/pending-output priorities and short/null result reads. The actual Unity runtime NativeRegisteredBattle.cs adapter is also compiled and executed against the rebuilt Windows DLL in three two-wave scenarios; 108 result/journal comparisons match the original and repeated disposal/creation does not exhaust catalog capacity. Nonzero enemy programs remain unsupported. No Unity engine, ARM64, real game resources, account state or iPhone execution was tested.'};
fs.writeFileSync(path.join(root, 'evidence/native/registered-battle-differential-tests.json'), JSON.stringify(record, null, 2) + '\n');
console.log(JSON.stringify(record, null, 2));
if (!record.success || !record.original.unchangedAfterTest) process.exitCode = 1;
