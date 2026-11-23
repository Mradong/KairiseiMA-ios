import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const out = path.join(root, 'test-output/order03-runtime-evidence', crypto.randomUUID());
const tables = path.join(out, 'tables'); fs.mkdirSync(tables, {recursive: true});
const hash = b => crypto.createHash('sha256').update(b).digest('hex');
const names = JSON.parse(fs.readFileSync(path.join(root, 'evidence/ios-package-search/order03-candidate-snapshot.json'), 'utf8')).tables.map(x => x.name);
const expected = {schemaVersion: 1, tables: names.map(name => { const bytes = Buffer.from('1,synthetic-test-only\n'); fs.writeFileSync(path.join(tables, name), bytes); return {name, bytes: bytes.length, sha256: hash(bytes)}; })};
const manifest = path.join(out, 'expected.json'); fs.writeFileSync(manifest, JSON.stringify(expected));
const script = path.join(root, 'scripts/Collect-Order03RuntimeEvidence.ps1');
const cases = [];
const wrapper = path.join(out, 'process-wrapper.ps1');
fs.writeFileSync(wrapper, 'param([string]$Collector,[string]$Tables,[string]$Expected,[string]$Destination)\n& $Collector -TablesRoot $Tables -ExpectedTables $Expected -Output $Destination -ServerBinary (Get-Process -Id $PID).Path -ServerProcessId $PID\n');
function run(name, wanted, args = [], selectedOutput = null) {
  const output = selectedOutput || path.join(out, name + '.json');
  const invocation = name === 'selected_powershell_process' ? [wrapper, '-Collector', script, '-Tables', tables, '-Expected', manifest, '-Destination', output] : [script, '-TablesRoot', tables, '-ExpectedTables', manifest, '-Output', output, ...args];
  const result = spawnSync('powershell.exe', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ...invocation], {encoding: 'utf8', windowsHide: true, timeout: 30000});
  if (wanted === 'rejected') { if (result.status === 0) throw Error('Unsafe input accepted: ' + name); cases.push({case: name, rejected: true}); return; }
  if (result.status !== 0) throw Error(name + ': ' + result.stdout + result.stderr);
  const r = JSON.parse(fs.readFileSync(output, 'utf8'));
  if (r.matchedTables !== wanted || r.totalTables !== 29 || !r.diskTablesStableDuringCollection || !r.effectiveLoadedContent.startsWith('UNKNOWN:') || r.battleContract.indexOf('NOT_TESTED:') !== 0) throw Error('Wrong report: ' + name);
  if (JSON.stringify(r).includes(out) || JSON.stringify(r).includes('synthetic-test-only')) throw Error('Private path/raw content leaked');
  if (name === 'selected_powershell_process' && (!r.sameObservedProcessDuringCollection || !r.binaryStableDuringCollection || !r.processBefore.imageMatchesSelectedBinary)) throw Error('Selected process not observed');
  cases.push({case: name, matched: r.matchedTables, loadedContentRemainsUnknown: true});
}
run('complete_synthetic_tables', 29);
run('selected_powershell_process', 29);
run('existing_evidence_not_overwritten', 'rejected', [], path.join(out, 'complete_synthetic_tables.json'));
const cardDirectory = path.join(out, 'card-tables'), cardNames = ['card.csv', 'buddy.csv', 'card_alchemist.csv'];
fs.mkdirSync(cardDirectory);
for (const name of cardNames) { fs.copyFileSync(path.join(tables, name), path.join(cardDirectory, name)); fs.unlinkSync(path.join(tables, name)); }
run('split_card_and_battle_directories', 29, ['-CardTablesRoot', cardDirectory]);
for (const name of cardNames) fs.copyFileSync(path.join(cardDirectory, name), path.join(tables, name));
fs.writeFileSync(path.join(tables, names[0]), '2,changed\n'); run('changed_table_detected', 28);
fs.unlinkSync(path.join(tables, names[1])); run('missing_table_detected', 27);
expected.tables[0].name = '../accounts.json'; fs.writeFileSync(manifest, JSON.stringify(expected)); run('non_allowlisted_path_rejected', 'rejected');
const valid = {...expected, tables: expected.tables.map((x, i) => ({...x, name: names[i]}))}; fs.writeFileSync(manifest, JSON.stringify(valid));
run('output_outside_handoff_rejected', 'rejected', [], path.join(root, '..', 'should-never-be-written.json'));
const report = {checkedAt: new Date().toISOString(), success: true, cases, collectorSha256: hash(fs.readFileSync(script)), scriptSha256: hash(fs.readFileSync(fileURLToPath(import.meta.url))), scope: 'Local Windows PowerShell with 29 explicitly synthetic CSV files, changed/missing file detection, path allowlist, output boundary and overwrite rejection. One case observes only its own PowerShell process and disk image, not a game server. No production data, network or account operation. Collector validation only; not deployed-server evidence.'};
fs.writeFileSync(path.join(root, 'evidence/ios-package-search/order03-runtime-collector-tests.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify(report, null, 2));
