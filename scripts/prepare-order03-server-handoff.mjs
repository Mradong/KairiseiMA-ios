import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = p => fs.readFileSync(path.join(root, p));
const json = p => JSON.parse(read(p).toString('utf8').replace(/^\uFEFF/, ''));
const sha = b => crypto.createHash('sha256').update(b).digest('hex');
const pin = 'd6619cd8acc8f8a3d60931e7b6404d0164198ef43684d2cdb5986160217d3661';
const sums = read('inputs/server-order03/Order03-Combat-Verified-20261123/SHA256SUMS-tables.txt');
if (sha(sums) !== pin) throw Error('Delivered hash list changed');
const tableIdentities = new Map(sums.toString('utf8').trim().split(/\r?\n/).map(line => {
  const m = line.match(/^([0-9a-f]{64})  tables\/([a-z][a-z0-9_]*\.csv)$/); if (!m) throw Error('Invalid table hash entry'); return [m[2], m[1]];
}));
const candidate = json('evidence/ios-package-search/order03-candidate-snapshot.json');
if (tableIdentities.size !== 29 || candidate.tables.length !== 29 || candidate.tables.some(x => tableIdentities.get(x.name) !== x.sha256)) throw Error('Reviewed table identity mismatch');
const collector = 'scripts/Collect-Order03RuntimeEvidence.ps1';
const tests = json('evidence/ios-package-search/order03-runtime-collector-tests.json');
if (!tests.success || tests.collectorSha256 !== sha(read(collector)) || tests.cases.length !== 8) throw Error('Collector validation is missing or stale');
const expectedPath = 'evidence/ios-package-search/order03-expected-tables.json';
fs.writeFileSync(path.join(root, expectedPath), JSON.stringify({schemaVersion: 1, sourceArchiveSha256: 'a44390313bb4a0d13897b70f35c313bafd380282189b44689e8a48340c6621ce', sourceHashListSha256: pin, tables: candidate.tables.map(x => ({name: x.name, bytes: x.bytes, sha256: x.sha256})), scope: 'Known delivered disk-file identities only; does not prove current server loading or original source-client version.'}, null, 2) + '\n');
const selected = ['顺序03补充取证说明.md', '顺序03服务端交付请求.md', collector, expectedPath,
  ...['order03-runtime-collector-tests.json','order03-public-version-evidence.json','order03-ios602-enums.json','order03-modern-metadata-enums.json','order03-ios602-table-compatibility.json','order03-modern-defaults.json'].map(x => 'evidence/ios-package-search/' + x)];
const suffix = new Date().toISOString().replace(/[-:.TZ]/g, '');
const relative = 'dist/order03-server-supplement-' + suffix, stage = path.join(root, relative), archive = stage + '.zip';
fs.mkdirSync(stage, {recursive: false});
const files = selected.map(p => {
  if (/(?:^|\/)(inputs|tools|test-output|source-review|\.git)\//.test(p) || /\.(exe|dll|ipa|ulf|alf|p12)$/i.test(p)) throw Error('Forbidden handoff path');
  const b = read(p), target = path.join(stage, p); fs.mkdirSync(path.dirname(target), {recursive: true}); fs.writeFileSync(target, b);
  return {path: p, bytes: b.length, sha256: sha(b)};
});
fs.writeFileSync(path.join(stage, 'SHA256SUMS.txt'), files.map(x => x.sha256 + '  ' + x.path).join('\n') + '\n');
// Pass only single-quoted literals to the PowerShell code, never shell substitutions.
const q = s => "'" + s.replaceAll("'", "''") + "'";
const code = "$ErrorActionPreference='Stop'; Add-Type -AssemblyName System.IO.Compression.FileSystem; " +
  '[IO.Compression.ZipFile]::CreateFromDirectory(' + q(stage) + ',' + q(archive) + '); ' +
  '$zip=[IO.Compression.ZipFile]::OpenRead(' + q(archive) + '); try { ' +
  '$verified=0; foreach($entry in $zip.Entries) { if($entry.FullName -eq "SHA256SUMS.txt"){continue}; ' +
  '$stream=$entry.Open(); try {$h=([BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash($stream))).Replace("-","").ToLowerInvariant()}finally{$stream.Dispose()}; ' +
  '$line=$h+"  "+$entry.FullName.Replace([char]92,[char]47); if(!(Get-Content -LiteralPath ' + q(path.join(stage, 'SHA256SUMS.txt')) + ' -Encoding UTF8).Contains($line)){throw "ZIP entry differs"};$verified++ }; ' +
  'if($verified -ne ' + selected.length + ' -or $zip.Entries.Count -ne ' + (selected.length + 1) + '){throw "ZIP entry count differs"}; Write-Output $verified } finally {$zip.Dispose()}';
const packed = spawnSync('powershell.exe', ['-NoProfile', '-Command', code], {encoding: 'utf8', windowsHide: true, timeout: 60000});
if (packed.status !== 0) throw Error('Archive creation/check failed: ' + packed.stdout + packed.stderr);
const receipt = {createdAt: new Date().toISOString(), archive: relative + '.zip', bytes: fs.statSync(archive).size, sha256: sha(fs.readFileSync(archive)), verifiedArchiveFiles: files.length, files,
  scriptSha256: sha(read(path.relative(root, fileURLToPath(import.meta.url)))), sentToServer: false, serverExecuted: false,
  scope: 'Prepared and locally verified supplemental handoff only. Contains a read-only collector, expected hashes, narrowed request and client evidence; no raw CSVs, original binaries/metadata, accounts, credentials, tools or live server changes. Server-side process/loading, quest and safe battle evidence remain required.'};
fs.writeFileSync(path.join(root, 'evidence/ios-package-search/order03-server-supplement.json'), JSON.stringify(receipt, null, 2) + '\n');
console.log(JSON.stringify({...receipt, files: undefined}, null, 2));
