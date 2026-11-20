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
 path.join(root,'ios-client/Assets/Recovery/Runtime/NativeRegisteredBattle.cs'), path.join(root,'ios-client/Assets/Recovery/Runtime/CnProtocol.cs')], {encoding: 'utf8'});
if (compile.status !== 0) throw new Error(compile.stdout + compile.stderr);
const run = spawnSync(exe, [original, rebuilt, path.join(root, 'tests/fixtures/native-session/tables.json'), path.join(root, 'test-output/native-session-differential'), path.join(root,'evidence/ios-package-search/ios-solo-start-fixture.json')], {encoding: 'utf8', timeout: 120000});
const record = {checkedAt: new Date().toISOString(), success: run.status === 0,
 counts: run.status === 0 ? JSON.parse(run.stdout.trim().replace(/^\uFEFF/, '')) : null,
 stderr: run.stderr.trim(), original: {sha256: expected, unchangedAfterTest: hash(original) === expected},
 rebuilt: {sha256: hash(rebuilt)},
 scope: 'Windows x64 synthetic registered mode-1 party: original-DLL status, full result CSV and cumulative journal comparisons. All 35 zero-ID enemy skill slots are tested. One nonzero normal action with null conditions selects ENEMY1 (itself) at rates 100 and 0; RANDOM and four profession selectors damage the expected living neutral player at rate 100. Skill slots 1 through 14 separately target the mercenary and cause observed damage. Slot 1 is tested with normal slot 0 also registered, but both actions executing is not proven. Dead-member fallback, special and death slots, other skill targets and nonempty triggers are unverified or rejected. Twelve cases cover player effects and two waves. Actual C# NativeRegisteredBattle adapter is compiled/executed against the rebuilt DLL with original result/journal comparisons, configuring continue, end turn 20 and initial burst 40; end turn is asserted in output. One scenario consumes end_turn=0 from an isolated service response through CnProtocol and the managed adapter; other battle data are synthetic. Continue and burst effects beyond matching results are not separately exercised. No real resources, Unity engine, ARM64 or iPhone execution was tested.'};
fs.writeFileSync(path.join(root, 'evidence/native/registered-battle-differential-tests.json'), JSON.stringify(record, null, 2) + '\n');
console.log(JSON.stringify(record, null, 2));
if (!record.success || !record.original.unchangedAfterTest) process.exitCode = 1;
