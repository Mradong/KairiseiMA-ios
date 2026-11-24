import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const output = path.join(root, 'test-output/order03-status');
fs.mkdirSync(output, { recursive: true });
const sha256 = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const original = path.join(root, 'inputs/native/kairisei_battlecore.dll');
const rebuilt = path.join(root, 'native/build/windows-amd64/kairisei_battlecore_rebuilt.dll');
const fixture = path.join(root, 'tests/fixtures/native-session/tables.json');
const source = path.join(root, 'tests/Order03StatusProbe.cs');
const raw = path.join(output, 'results.json');
const expectedOriginal = JSON.parse(fs.readFileSync(path.join(root, 'evidence/native/go-symbols-summary.json'), 'utf8')).sha256;
if (sha256(original) !== expectedOriginal) throw new Error('Original DLL changed');

const json = path.join(root, 'tools/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll');
const exe = path.join(output, 'probe.exe');
fs.copyFileSync(json, path.join(output, 'Newtonsoft.Json.dll'));
const compiler = path.join(process.env.WINDIR, 'Microsoft.NET/Framework64/v4.0.30319/csc.exe');
const build = spawnSync(compiler, ['/nologo', '/platform:x64', '/out:' + exe, '/reference:' + json, source], { encoding: 'utf8', windowsHide: true });
if (build.status !== 0) throw new Error(build.stdout + build.stderr);
const run = spawnSync(exe, [original, rebuilt, fixture, raw], { encoding: 'utf8', windowsHide: true, timeout: 120000 });
if (run.status !== 0) throw new Error(run.stderr || run.stdout);
const cases = JSON.parse(fs.readFileSync(raw, 'utf8'));
const originalValid = cases.length === 192 && cases.every(item =>
  item.original.createStatus === 0 && item.original.functionID === item.expectedFunctionID &&
  item.original.branchIndex === (item.expectedFunctionID === 889 ? 1 : 0));
const rebuiltMatches = cases.every(item => JSON.stringify(item.rebuilt) === JSON.stringify(item.original));
const selectedCount = cases.filter(item => item.original.functionID === 889).length;
const fallbackCount = cases.filter(item => item.original.functionID === 888).length;
const report = {
  checkedAt: new Date().toISOString(),
  success: originalValid && rebuiltMatches && selectedCount === 32 && fallbackCount === 160,
  cases,
  summary: { cases: cases.length, originalValid, rebuiltMatches, selectedCount, fallbackCount },
  originalSha256: sha256(original),
  rebuiltSha256: sha256(rebuilt),
  fixtureSha256: sha256(fixture),
  probeSha256: sha256(source),
  scriptSha256: sha256(fileURLToPath(import.meta.url)),
  bridgeSourceSha256: sha256(path.join(root, 'native/bridge/skill_query.go')),
  conversionSource: 'legacy-unity/ExportedProject/Assets/Scripts/Assembly-CSharp/SkillDataContainer.cs',
  scope: 'Windows x64 synthetic registered battles. Member 1 selects and executes ATK_UP_FIXED or ATK_BREAK_FIXED at priority 1, then member 2 executes a conditional card at priority 2. SELF/TARGET/USER_SIDE/ENEMY_SIDE buff and debuff conditions cover state applied or absent, matching first/second selectors, mismatches, NULL and a numeric third parameter, and both condition slots. Complete output traces and journals are compared with the original DLL. No actual service skill, provenance/version compatibility, Unity, ARM64 or device proof.'
};
const observe = process.argv.includes('--observe');
const evidence = path.join(root, 'evidence/ios-package-search/order03-status' + (observe ? '-before-fix' : '') + '.json');
fs.writeFileSync(evidence, JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({ ...report, cases: cases.length }, null, 2));
if (!observe && !report.success) process.exitCode = 1;
