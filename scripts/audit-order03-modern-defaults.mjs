import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
import {readIl2cppMetadataForLinkingSteps} from '../tools/rea/node_modules/rea-agents/dist/il2cpp/Il2cppMetadataFile.js';
import {linkIl2cppBinarySteps} from '../tools/rea/node_modules/rea-agents/dist/il2cpp/Il2cppBinaryFile.js';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = p => fs.readFileSync(path.resolve(root, p));
const json = p => JSON.parse(read(p).toString('utf8').replace(/^\uFEFF/, ''));
const hash = b => crypto.createHash('sha256').update(b).digest('hex');
const need = (value, message) => { if (!value) throw Error(message); };
const imagePath = path.resolve(root, '../GameAssembly.dll');
const metadataPath = path.resolve(root, '../ClientRebuild6_Data/il2cpp_data/Metadata/global-metadata.dat');
const image = read(imagePath), metadata = read(metadataPath);
const imageHash = 'ace0b9692a83dc6c1fbc0c2e6663efbc0a0e41bc682eee03c86eba1d5be7834b';
const metadataHash = 'fd31c0a95cc4ef80e99643c0dff8afb2ef516625ec86b3e4863ad48a54596294';
need(hash(image) === imageHash && hash(metadata) === metadataHash, 'Original Windows input changed');
const exhaust = g => { let step; do { step = g.next(); } while (!step.done); return step.value; };
const {metadata: m, link: facts} = exhaust(readIl2cppMetadataForLinkingSteps(metadata));
const native = exhaust(linkIl2cppBinarySteps(image, m, facts));
need(m.metadata_version === 39 && native.image_base === 0x180000000n && native.architecture === 'amd64', 'Unexpected metadata/native layout');
const pe = image.readUInt32LE(0x3c), sectionStart = pe + 24 + image.readUInt16LE(pe + 20);
const sections = Array.from({length: image.readUInt16LE(pe + 6)}, (_, i) => {
  const at = sectionStart + i * 40;
  return {rva: image.readUInt32LE(at + 12), size: image.readUInt32LE(at + 16), file: image.readUInt32LE(at + 20)};
});
function file(rva, size = 1) {
  const matches = sections.filter(s => rva >= s.rva && rva + size <= s.rva + s.size);
  need(matches.length === 1, 'Ambiguous/unmapped PE RVA');
  const at = matches[0].file + rva - matches[0].rva;
  need(at + size <= image.length, 'Truncated PE section'); return at;
}
const evidence = [], methods = [];
function check(rva, hex, label) {
  const expected = Buffer.from(hex, 'hex');
  need(image.subarray(file(rva, expected.length), file(rva, expected.length) + expected.length).equals(expected), 'Native pattern differs: ' + label);
  evidence.push({label, rva: '0x' + rva.toString(16), bytes: hex});
}
function method(type, name, rva) {
  const matches = m.methods.flatMap((x, index) => {
    const owner = m.types[x.declaring_type], qualified = owner?.namespace ? owner.namespace + '.' + owner.name : owner?.name;
    return qualified === type && x.name === name && native.method_addresses[index].rva === '0x' + rva.toString(16) ? [index] : [];
  });
  need(matches.length === 1, 'Method token/pointer mismatch: ' + type + '.' + name);
  const index = matches[0];
  methods.push({type, name, methodIndex: index, token: '0x' + facts.methodTokens[index].toString(16), ...native.method_addresses[index]});
  return rva;
}
function branch(at, opcode, expectedRva, label) {
  need(image.readUInt8(file(at)) === opcode && at + 5 + image.readInt32LE(file(at + 1, 4)) === expectedRva, 'Native branch differs: ' + label);
  evidence.push({label, rva: '0x' + at.toString(16), targetRva: '0x' + expectedRva.toString(16)});
}
method('SkillDataContainer', 'getRoleParam', 0x380370);
method('Global', 'parseInt', 0x550080); method('Global', 'parseLong', 0x5500f0);
method('System.Int32', 'TryParse', 0x1890fb0); method('System.Int64', 'TryParse', 0x1891d70);
method('System.String', 'op_Equality', 0x16e1cc0); method('System.String', 'IsNullOrEmpty', 0x16e4260);
method('CSV', 'getParamCount', 0x4cd960); method('CSV', 'isParamEmpty', 0x4cdee0);
method('CSV', 'getLong', 0x4cd8d0); method('CSV', 'popLong', 0x4ceaa0);
method('EnemyAIOrderDataContainer', 'ParseTrigger', 0x2f9e70);

// Independently decode the extra role-rule enum through the existing C# reader.
const out = path.join(root, 'test-output/order03-modern-defaults'); fs.mkdirSync(out, {recursive: true});
const jsonDll = path.join(root, 'tools/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll'); fs.copyFileSync(jsonDll, path.join(out, 'Newtonsoft.Json.dll'));
const exe = path.join(out, 'MetadataProbe.exe'), source = path.join(root, 'tests/Order03MetadataEnumsProbe.cs'), enumFile = path.join(out, 'rule-and-trigger-enums.json');
const build = spawnSync(path.join(process.env.WINDIR, 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'), ['/nologo', '/platform:x64', '/out:' + exe, '/reference:' + jsonDll, source], {encoding: 'utf8', windowsHide: true});
need(build.status === 0, 'Metadata reader compile failed: ' + build.stdout + build.stderr);
const run = spawnSync(exe, [metadataPath, 'SKILL_ROLE_PARAM,ENEMY_AI_ORDER_TRIGGER', enumFile], {encoding: 'utf8', windowsHide: true, timeout: 60000});
need(run.status === 0, 'Independent enum reader failed: ' + run.stderr);
const enums = json(enumFile), rules = enums.find(x => x.name === 'SKILL_ROLE_PARAM').values;
const valueOrdinal = rules.find(x => x.name === 'VALUE')?.value;
need(valueOrdinal === 1, 'Unexpected VALUE rule ordinal');
check(0x380444, '83fa0c772c', 'role-rule dispatch upper bound');
check(0x380453, '8b8c82b80738004803caffe1', 'role-rule dispatch table');
const valueTarget = image.readUInt32LE(file(0x3807b8 + valueOrdinal * 4, 4));
need(valueTarget === 0x38045f, 'VALUE dispatch destination differs');
branch(0x38046c, 0xe8, 0x16e1cc0, 'VALUE string equality');
branch(0x38048f, 0xe8, 0x16e4260, 'VALUE null/empty check');
check(0x380471, '84c07415b8ffffffff', 'special literal returns minus one');
const literalSlot = 0x38045f + 7 + image.readInt32LE(file(0x38045f + 3, 4));
const encoded = image.readUInt32LE(file(literalSlot, 4));
need((encoded >>> 29) === 5 && (encoded & 1) === 1, 'Expected encoded string-literal usage');
const literalIndex = (encoded & 0x1ffffffe) >>> 1;
need(m.string_literals[literalIndex]?.value === 'ALL', 'VALUE sentinel literal is not ALL');
check(0x3804b1, '4533c033d2488bcb', 'VALUE passes explicit default zero');
branch(0x3804c8, 0xe9, 0x550080, 'VALUE calls Global.parseInt');
check(0x550091, '8bda488bf9', 'parseInt saves caller default');
branch(0x5500d3, 0xe8, 0x1890fb0, 'parseInt calls Int32.TryParse');
check(0x5500d8, '84c08bc374048b442438', 'parseInt chooses parsed value or saved default');
branch(0x550145, 0xe8, 0x1891d70, 'parseLong calls Int64.TryParse');
check(0x55014a, '84c0488bc37405488b442430', 'parseLong chooses parsed value or saved default');
check(0x4cdf1e, '3bd8488b5c24307d37', 'missing CSV column enters empty branch');
check(0x4cdf5e, 'b0014883c4205fc3', 'empty-column predicate returns true');
branch(0x4cd901, 0xe8, 0x4cdee0, 'getLong checks empty column');
check(0x4cd906, '84c07542', 'getLong branches to caller default when empty');
check(0x4cd94c, '488b5c2430488bc74883c4205fc3', 'getLong returns saved default');
branch(0x4cd947, 0xe9, 0x5500f0, 'nonempty getLong calls parseLong');
check(0x4ceab1, '488bf2488bf9', 'popLong saves caller default');
branch(0x4cead6, 0xe8, 0x4cdee0, 'popLong checks empty column');
check(0x4ceadb, '84c0753b', 'empty popLong skips parsing');
check(0x4ceb2e, '3bd8488bc67d16', 'popLong keeps return value and does not advance beyond row end');
branch(0x4ceb12, 0xe8, 0x5500f0, 'nonempty popLong calls parseLong');

const triggers = enums.find(x => x.name === 'ENEMY_AI_ORDER_TRIGGER').values;
check(0x2f9faf, '83c3fd83fb44', 'trigger dispatch subtracts three and checks unsigned range');
check(0x2f9fc5, '410fb6840758b02f00418b8c87f0af2f004903cfff e1'.replaceAll(' ', ''), 'trigger two-level dispatch tables');
const triggerCases = ['HEAL_TOTAL', 'DAMAGE', 'MAGIC_DAMAGE', 'PHYSIC_DAMAGE', 'USER_DEAD'].map(name => {
  const ordinal = triggers.find(x => x.name === name)?.value; need(Number.isInteger(ordinal), 'Missing trigger enum');
  const index = ordinal - 3;
  const group = index < 0 || index > 68 ? null : image.readUInt8(file(0x2fb058 + index));
  const target = group === null ? 0x2faf18 : image.readUInt32LE(file(0x2faff0 + group * 4, 4));
  need(target === (name === 'USER_DEAD' ? 0x2fab37 : 0x2faf18), 'Unexpected trigger path: ' + name);
  return {name, ordinal, selectorGroup: group, targetRva: '0x' + target.toString(16), firstParameterSpecial: name === 'USER_DEAD'};
});
check(0x2faf33, '4533c033d2488bcd', 'generic numeric trigger passes default zero');
branch(0x2faf3e, 0xe8, 0x4ceaa0, 'generic numeric trigger calls popLong');
check(0x2fab5c, '85db7420', 'USER_DEAD handles its first selector separately');
check(0x2fab60, '4533c033d2', 'USER_DEAD numeric tail passes default zero');
branch(0x2fab65, 0xe8, 0x4ceaa0, 'USER_DEAD numeric tail calls popLong');

const pkg = 'inputs/server-order03/Order03-Combat-Verified-20261123/';
const snapshot = json('evidence/ios-package-search/order03-candidate-snapshot.json');
const sourceHashes = {};
function table(name) {
  const p = pkg + 'tables/' + name, bytes = read(p), expected = snapshot.tables.find(x => x.name === name);
  need(expected && hash(bytes) === expected.sha256, 'Delivered table changed: ' + name);
  sourceHashes[name] = hash(bytes);
  const text = bytes.toString('utf8').trimEnd();
  need(!text.includes('"'), 'Quoted rows require a full CSV reader: ' + name);
  return text.split(/\r?\n/).map(line => line.split(','));
}
const roleRows = table('skill_role_enemy.csv'), ruleRows = table('skill_role_param_rule.csv'), aiRows = table('enemy_ai_order.csv');
const ruleMap = new Map(ruleRows.map(row => [row[0], row]));
const rawAudit = json(pkg + 'audit/raw-role-parameters.json');
need(rawAudit.numeric_problems.length === 25, 'Unexpected VALUE observation count');
for (const x of rawAudit.numeric_problems) {
  const row = roleRows[x.record - 1], rule = ruleMap.get(x.function), parameter = x.column - 20;
  need(x.table === 'skill_role_enemy.csv' && row?.[0] === x.role_set_id && row[8] === x.function && row[x.column] === x.raw_value && parameter >= 0 && parameter < 10 && rule?.[3 + parameter] === 'VALUE', 'Raw VALUE location/rule mismatch');
  need(x.raw_value !== 'ALL' && (!/^[+-]?\d+$/.test(x.raw_value) || BigInt(x.raw_value) < -2147483648n || BigInt(x.raw_value) > 2147483647n), 'Unexpected valid Int32 observation');
}
const warnings = json(pkg + 'audit/native-loader-result.json').additional_registry_checks.raw_ai_shape_warnings;
need(warnings.length === 36, 'Unexpected short AI count');
const aiMap = new Map(aiRows.map(row => [row[0], row]));
let missingNumericTailFields = 0;
for (const w of warnings) {
  const row = aiMap.get(w.id); need(row && row.length === w.raw_columns && row[29] === w.trigger && row.length >= 31 && row.length < 38 && triggerCases.some(x => x.name === w.trigger), 'Raw AI shape mismatch');
  missingNumericTailFields += 38 - row.length;
}
need(missingNumericTailFields === 206, 'Unexpected missing numeric tail count');
need(hash(read(imagePath)) === imageHash && hash(read(metadataPath)) === metadataHash, 'Original input changed during audit');
const report = {
  checkedAt: new Date().toISOString(), staticChecksPassed: true, runtimeExecuted: false,
  source: {binary: 'GameAssembly.dll', binarySha256: imageHash, metadata: 'ClientRebuild6_Data/il2cpp_data/Metadata/global-metadata.dat', metadataSha256: metadataHash, platform: 'Windows amd64', unity: '6000.3.26f1'},
  scriptSha256: hash(read(fileURLToPath(import.meta.url))), independentEnumReaderSha256: hash(read(source)),
  tooling: ['tools/rea/node_modules/rea-agents/dist/il2cpp/Il2cppMetadataFile.js', 'tools/rea/node_modules/rea-agents/dist/il2cpp/Il2cppBinaryFile.js'].map(file => ({file, sha256: hash(read(file))})),
  auditSourceHashes: {rawRoleObservations: hash(read(pkg + 'audit/raw-role-parameters.json')), shortAiObservations: hash(read(pkg + 'audit/native-loader-result.json'))},
  methods, instructionChecks: evidence, valueRule: {ordinal: valueOrdinal, targetRva: '0x' + valueTarget.toString(16), sentinel: 'ALL', sentinelResult: -1, literalIndex, normalPath: 'Int32.TryParse with explicit default 0'},
  rawValueLocationsAndRulesChecked: rawAudit.numeric_problems.length,
  rawValueCategories: Object.fromEntries([...new Set(rawAudit.numeric_problems.map(x => x.raw_value))].map(value => [value, rawAudit.numeric_problems.filter(x => x.raw_value === value).length])),
  shortAiRowsChecked: warnings.length, missingNumericTailFields, triggerCases, sourceHashes,
  scope: 'Static original Windows rebuild analysis only. Metadata tokens, module method pointers and binary instruction bytes cross-check helper calls, VALUE dispatch and ALL literal, invalid Int32 fallback, out-of-row CSV defaults and numeric trigger-tail paths. Original raw locations and VALUE rules rechecked. This is not execution of these 25 values/36 rows through IL2CPP, original 6.0.2 behavior, raw CSV-to-string-array parser equivalence, runtime trigger-array length, USER_DEAD selector semantics, full AI/effects, shared server/client original version, live service, Unity/iOS/ARM64/device proof. Static behavior applies when valid CSV state requests these numeric slots; the raw tables and candidate are not modified.'
};
fs.writeFileSync(path.join(root, 'evidence/ios-package-search/order03-modern-defaults.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({...report, instructionChecks: report.instructionChecks.length, methods: report.methods.map(x => ({type: x.type, name: x.name, rva: x.rva}))}, null, 2));
