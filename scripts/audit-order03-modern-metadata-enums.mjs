import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
import {readIl2cppMetadataForLinkingSteps} from '../tools/rea/node_modules/rea-agents/dist/il2cpp/Il2cppMetadataFile.js';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = p => fs.readFileSync(path.resolve(root, p));
const sha = p => crypto.createHash('sha256').update(read(p)).digest('hex');
const json = p => JSON.parse(read(p).toString('utf8').replace(/^\uFEFF/, ''));
// Pin the original inventory identity explicitly; the original metadata is a
// separately supplied read-only input and is never copied to the upload tree.
const record = {path: 'ClientRebuild6_Data/il2cpp_data/Metadata/global-metadata.dat', bytes: 11223492, sha256: 'fd31c0a95cc4ef80e99643c0dff8afb2ef516625ec86b3e4863ad48a54596294'};
const metadataPath = path.resolve(root, '..', record.path), bytes = read(metadataPath);
if (sha(metadataPath) !== record.sha256 || bytes.length !== record.bytes) throw Error('Original Windows metadata changed');
const generator = readIl2cppMetadataForLinkingSteps(bytes);
let step; do { step = generator.next(); } while (!step.done);
const m = step.value.metadata;
if (m.metadata_version !== 39) throw Error('Expected metadata v39');
const section = name => m.sections.find(x => x.name === name);
const typeWidth = m.index_widths.type, typeSize = section('typeDefinitions').size / m.types.length;
const int32s = m.types.flatMap((t, i) => t.name === 'Int32' && t.namespace === 'System' ? [i] : []);
if (int32s.length !== 1) throw Error('Ambiguous System.Int32');
const indexAt = (offset, width) => width === 2 ? bytes.readUInt16LE(offset) : width === 4 ? bytes.readUInt32LE(offset) : bytes.readUInt8(offset);
const primitive = indexAt(section('typeDefinitions').offset + int32s[0] * typeSize + 8, typeWidth);
const defs = section('fieldDefaultValues'), data = section('fieldAndParameterDefaultValueData');
const defaultSize = defs.size / defs.count;
if (defaultSize !== 8 + typeWidth) throw Error('Unexpected default record width');
const defaults = new Map();
for (let i = 0; i < defs.count; i++) {
  const at = defs.offset + i * defaultSize, field = bytes.readInt32LE(at), type = indexAt(at + 4, typeWidth), offset = bytes.readInt32LE(at + 4 + typeWidth);
  if (field < 0 || field >= m.fields.length || defaults.has(field) || offset < -1 || offset >= data.size) throw Error('Invalid field default');
  defaults.set(field, {at, type, offset});
}
function compressedInt(offset) {
  let encoded = bytes[data.offset + offset], count = 1;
  if (encoded >= 0x80) {
    if (encoded >= 0xc0 || offset + 2 > data.size) throw Error('Enum constant exceeds supported one/two-byte compressed form');
    encoded = (encoded & 0x3f) * 256 + bytes[data.offset + offset + 1]; count = 2;
  }
  return {value: encoded & 1 ? -((encoded >>> 1) + 1) : encoded >>> 1, count};
}
const candidatePath = 'test-output/order03-candidate/DO-NOT-DEPLOY-CombatTables.json';
const candidate = json(candidatePath), receipt = json('evidence/ios-package-search/order03-candidate-snapshot.json');
if (sha(candidatePath) !== receipt.snapshotSha256) throw Error('Candidate changed');
const families = candidate.enums.map(e => {
  const matches = m.types.flatMap((t, i) => t.name === e.name && !t.namespace ? [i] : []);
  if (matches.length !== 1) throw Error('Ambiguous family: ' + e.name);
  const type = matches[0], typeOffset = section('typeDefinitions').offset + type * typeSize;
  if ((bytes.readUInt32LE(typeOffset + typeSize - 8) & 3) !== 3) throw Error('Selected type is not enum');
  const values = [], sentinels = [];
  for (let field = 0; field < m.fields.length; field++) {
    const f = m.fields[field]; if (f.declaring_type !== type) continue;
    const d = defaults.get(field);
    if (f.name === 'value__') { if (d) throw Error('Backing field has default'); continue; }
    if (!d || d.type !== primitive || d.offset < 0) throw Error('Enum constant is not System.Int32');
    const decoded = compressedInt(d.offset);
    const value = {name: f.name, value: decoded.value, fieldIndex: field, defaultRecordOffset: d.at, dataOffset: data.offset + d.offset, encodedBytes: bytes.subarray(data.offset + d.offset, data.offset + d.offset + decoded.count).toString('hex')};
    if (f.name === 'sizeof_' + e.name) sentinels.push(value); else values.push(value);
  }
  if (JSON.stringify(values.map(v => ({name: v.name, value: v.value}))) !== JSON.stringify(e.values)) throw Error('Candidate differs from raw metadata: ' + e.name);
  return {name: e.name, typeIndex: type, typeOffset, values, excludedSentinels: sentinels};
});
const out = path.join(root, 'test-output/order03-modern-metadata'); fs.mkdirSync(out, {recursive: true});
const jsonDll = path.join(root, 'tools/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll'); fs.copyFileSync(jsonDll, path.join(out, 'Newtonsoft.Json.dll'));
const exe = path.join(out, 'ModernMetadataEnumsProbe.exe'), other = path.join(out, 'independent-csharp-enums.json');
const source = 'tests/Order03MetadataEnumsProbe.cs';
const compile = spawnSync(path.join(process.env.WINDIR, 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'), ['/nologo', '/platform:x64', '/out:' + exe, '/reference:' + jsonDll, path.join(root, source)], {encoding: 'utf8', windowsHide: true});
if (compile.status !== 0) throw Error(compile.stdout + compile.stderr);
const run = spawnSync(exe, [metadataPath, families.map(x => x.name).join(','), other], {encoding: 'utf8', windowsHide: true, timeout: 60000});
if (run.status !== 0) throw Error(run.stdout + run.stderr);
if (JSON.stringify(json(other)) !== JSON.stringify(families.map(f => ({name: f.name, values: f.values.map(v => ({name: v.name, value: v.value, dataOffset: v.dataOffset}))})))) throw Error('Independent readers disagree');
const report = {
  checkedAt: new Date().toISOString(), success: true,
  source: {file: record.path, metadataSha256: record.sha256, metadataBytes: record.bytes, metadataVersion: 39, unityVersion: '6000.3.26f1', provenance: 'User-provided Windows ClientRebuild6 input pinned by initial inventory; this is a reconstructed client, not evidence of an official source-client game version.'},
  candidateSha256: receipt.snapshotSha256, indexWidths: m.index_widths, primitiveInt32TypeIndex: primitive,
  structuralReader: {file: 'tools/rea/node_modules/rea-agents/dist/il2cpp/Il2cppMetadataFile.js', sha256: sha('tools/rea/node_modules/rea-agents/dist/il2cpp/Il2cppMetadataFile.js')},
  scriptSha256: sha(fileURLToPath(import.meta.url)), independentReaderSha256: sha(source), independentCsharpAgreement: true,
  candidateEnumAgreement: true, enumFamilies: families.length, enumConstants: families.reduce((n, x) => n + x.values.length, 0), families,
  scope: 'Original pinned Windows rebuild metadata only: all 23 candidate enum families and integer values match direct constant decoding and an independent C# reader. Supports exact Windows rebuild provenance for candidate numbers, including SELF_STYLE=110 and SELF_ENEMY_AI_VAR=111. Does not establish an official original-client version, common origin/version with the server CSVs, recovered method-body semantics, complete native bridge behavior, service deployment, Unity/iOS/device operation, or permission to release the candidate. Compressed integer reader intentionally accepts only one/two-byte forms used by the selected constants; other forms fail.'
};
if (sha(metadataPath) !== record.sha256) throw Error('Original metadata changed');
fs.writeFileSync(path.join(root, 'evidence/ios-package-search/order03-modern-metadata-enums.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({...report, families: families.map(x => ({name: x.name, constants: x.values.length, keyValues: x.values.filter(v => /^(SELF_STYLE|SELF_ENEMY_AI_VAR)$/.test(v.name))}))}, null, 2));
