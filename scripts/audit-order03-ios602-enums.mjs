import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
import {readV21Int32Enums} from './il2cpp-v21-enums.mjs';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const out = path.join(root, 'test-output/order03-metadata');
fs.mkdirSync(out, {recursive: true});
const sha = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const metadataPath = 'inputs/ios/cn602/Data/Managed/Metadata/global-metadata.dat';
const bytes = fs.readFileSync(path.join(root, metadataPath));
const receipt = JSON.parse(fs.readFileSync(path.join(root, 'evidence/ios-package-search/cn602-import.json'), 'utf8').replace(/^\uFEFF/, ''));
const record = receipt.files.find(x => x.path === 'Data/Managed/Metadata/global-metadata.dat');
if (!record || record.sha256 !== sha(bytes) || record.bytes !== bytes.length) throw Error('Imported metadata hash differs');
const plist = fs.readFileSync(path.join(root, 'inputs/ios/cn602/Info.plist'));
if (sha(plist) !== receipt.files.find(x => x.path === 'Info.plist')?.sha256) throw Error('Imported plist hash differs');
const plistText = plist.toString('utf8');
const plistString = key => plistText.match(new RegExp('<key>' + key + '</key>\\s*<string>([^<]+)</string>'))?.[1];
if (plistString('CFBundleShortVersionString') !== '6.0.2' || plistString('CFBundleVersion') !== '60200' || plistString('CFBundleIdentifier') !== 'com.netease.glxbwysw') throw Error('Unexpected IPA identity');
const candidate = JSON.parse(fs.readFileSync(path.join(root, 'evidence/ios-package-search/order03-candidate-snapshot.json'), 'utf8'));
const names = candidate.enumFamilies.map(x => x.name);
const parsed = readV21Int32Enums(bytes, names);
const schema = parsed.families.map(x => ({name: x.name, values: x.values.map(v => ({name: v.name, value: v.value}))}));
fs.writeFileSync(path.join(out, 'ios602-enums.json'), JSON.stringify(schema, null, 2) + '\n');
const json = path.join(root, 'tools/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll');
fs.copyFileSync(json, path.join(out, 'Newtonsoft.Json.dll'));
const exe = path.join(out, 'MetadataEnumsProbe.exe'), otherPath = path.join(out, 'independent-csharp-enums.json');
const source = path.join(root, 'tests/Order03MetadataEnumsProbe.cs');
const compile = spawnSync(path.join(process.env.WINDIR, 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'),
  ['/nologo', '/platform:x64', '/out:' + exe, '/reference:' + json, source], {encoding: 'utf8', windowsHide: true});
if (compile.status !== 0) throw Error(compile.stdout + compile.stderr);
const run = spawnSync(exe, [path.join(root, metadataPath), names.join(','), otherPath], {encoding: 'utf8', windowsHide: true, timeout: 60000});
if (run.status !== 0) throw Error(run.stdout + run.stderr);
const other = JSON.parse(fs.readFileSync(otherPath, 'utf8'));
const normalized = parsed.families.map(x => ({name: x.name, values: x.values.map(v => ({name: v.name, value: v.value, dataOffset: v.dataOffset}))}));
if (JSON.stringify(other) !== JSON.stringify(normalized)) throw Error('Independent metadata readers disagree');

const corruptionChecks = [];
function rejects(label, mutate, selected = names) {
  const changed = Buffer.from(bytes); mutate(changed);
  try { readV21Int32Enums(changed, selected); } catch (e) { corruptionChecks.push({case: label, rejected: true, reason: e.message}); return; }
  throw Error('Corrupt input accepted: ' + label);
}
rejects('wrong magic', b => b.writeUInt32LE(0, 0));
rejects('unsupported version', b => b.writeInt32LE(24, 4));
rejects('out-of-bounds section', b => b.writeInt32LE(b.length, 24));
rejects('overlapping section', b => b.writeInt32LE(parsed.sections.strings.offset, 32));
rejects('nonintegral type record', b => b.writeInt32LE(parsed.sections.typeDefinitions.size - 1, 164));
rejects('bad field ownership', b => b.writeInt32LE(-1, parsed.families[0].typeOffset + 64));
rejects('bad string index', b => b.writeInt32LE(parsed.sections.strings.size, parsed.families[0].typeOffset));
rejects('non-enum type', b => b.writeUInt32LE(1, parsed.families[0].typeOffset + 112));
const first = parsed.families[0].values[0];
rejects('wrong constant primitive', b => b.writeInt32LE(-1, first.defaultRecordOffset + 4));
rejects('constant outside default data', b => b.writeInt32LE(parsed.sections.defaultValueData.size - 3, first.defaultRecordOffset + 8));
rejects('missing selected family', () => {}, ['NOT_A_REAL_ENUM']);
const report = {
  checkedAt: new Date().toISOString(), success: true,
  source: {ipaSha256: receipt.sourceSha256, version: '6.0.2', build: '60200', bundleIdentifier: 'com.netease.glxbwysw', metadataPath, metadataSha256: sha(bytes), metadataBytes: bytes.length, plistSha256: sha(plist)},
  layoutReference: {repository: 'https://github.com/Perfare/Il2CppDumper', commit: '4741d46ba9cd6159c5d853eb9d6fc48b4bfa2b1a', file: 'Il2CppDumper/Il2Cpp/MetadataClass.cs', version: 21},
  parserSha256: sha(fs.readFileSync(path.join(root, 'scripts/il2cpp-v21-enums.mjs'))),
  csharpReaderSha256: sha(fs.readFileSync(source)),
  scriptSha256: sha(fs.readFileSync(fileURLToPath(import.meta.url))),
  counts: parsed.counts, enumFamilies: schema.length, enumConstants: schema.reduce((n, x) => n + x.values.length, 0),
  independentCsharpAgreement: true, corruptionChecks,
  schemaSha256: sha(fs.readFileSync(path.join(out, 'ios602-enums.json'))),
  families: parsed.families,
  scope: 'Metadata-only extraction of Int32 enum constants in the imported 6.0.2/60200 IPA. Header sections, image/type/field ownership, enum bitfields, System.Int32 default-value types, strings and constant byte offsets checked; independent C# reader agrees. No encrypted executable, invalid historical dump.cs, native type registration, code body or game execution used. Establishes these original IPA constants, not compatibility with delivered modern tables or the rebuilt client, modern parameter conversion, service deployment, Unity/ARM64/device execution. No candidate or product snapshot is replaced.'
};
if (sha(fs.readFileSync(path.join(root, metadataPath))) !== record.sha256) throw Error('Metadata changed');
fs.writeFileSync(path.join(root, 'evidence/ios-package-search/order03-ios602-enums.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({...report, families: report.families.map(x => ({name: x.name, count: x.values.length, keyValues: x.values.filter(v => /^(SELF_ENEMY_AI_VAR|SELF_STYLE)$/.test(v.name))}))}, null, 2));
