import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const output = fs.mkdtempSync(path.join(root, 'test-output/order03-selection-tests-'));
const source = process.env.ORDER03_TEST_SCRIPT || path.join(root, 'scripts/Collect-Order03RuntimeSelection.ps1');
const fixture = JSON.parse(fs.readFileSync(path.join(root, 'evidence/ios-package-search/order03-expected-tables.json'), 'utf8'));
for (const table of fixture.tables) {
  const directory = path.join(output, ['card.csv','buddy.csv','card_alchemist.csv'].includes(table.name) ? 'cards' : 'combat');
  fs.mkdirSync(directory, { recursive: true });
  fs.writeFileSync(path.join(directory, table.name), '1,SYNTHETIC\n');
}
fs.writeFileSync(path.join(output, 'battle.json'), '{"replays":[]}');
fs.writeFileSync(path.join(output, 'cn602-normal-quest-runtime.json'), '{"replays":[]}');
const manifest = path.join(output, 'resource-set.json');
fs.writeFileSync(manifest, JSON.stringify({
  entrypoints: { 'cn-combat-card-master':'cards/card.csv', 'cn-combat-master-root':'combat', 'cn-battle-master':'battle.json', 'cn-save-seed':'PRIVATE-SEED-LOCATOR' },
  unrelated: { password: 'SYNTHETIC-SECRET-MUST-STAY-PRIVATE', files: Array(1000).fill({ name:'unused' }) }
}));
fs.writeFileSync(path.join(output, 'platform-config.json'), JSON.stringify({ platform_contents:['content/ios.json'], secret:'SYNTHETIC-PRIVATE-CONFIG' }));
fs.writeFileSync(path.join(output, 'ios.json'), JSON.stringify({ platform:'iOS', unity_version:'6000.3.26f1', content_id:'a'.repeat(64) }));
const verifiedSource = JSON.parse(fs.readFileSync(path.join(root, 'evidence/ios-package-search/order03-reviewed-patch-reproduction.json'), 'utf8'));
const verifiedExe = path.join(root, verifiedSource.build.executable);
const command = [
  '. $env:ORDER03_SCRIPT',
  '$words=[Order03SelectionNative]::Arguments(' + "'" + '"C:\\Program Files\\kairi-server.exe" -resource-set "C:\\data folder\\resource-set.json" -platform-content=C:\\profile\\ios.json -cn-save-path C:\\private\\save.json -resource-set=C:\\final\\resource-set.json' + "')",
  '$selected=Get-ResourceArgumentSelections $words',
  '$resources=Read-SelectedResourceFiles $env:ORDER03_MANIFEST',
  '$platforms=[Order03SelectionNative]::PlatformPaths((Join-Path $env:ORDER03_OUTPUT "platform-config.json"))',
  '$unsafeRejected=$false; try { Resolve-SelectedResource $env:ORDER03_OUTPUT "../private.json" } catch { $unsafeRejected=$true }',
  '$report=@{ selected=$selected; resources=$resources; platforms=$platforms; unsafeRejected=$unsafeRejected }',
  '[IO.File]::WriteAllText((Join-Path $env:ORDER03_OUTPUT "results.json"),(ConvertTo-Json -InputObject $report -Depth 15),[Text.UTF8Encoding]::new($false))',
  '$global:order03FixtureStart=[DateTime]::UtcNow',
  'function Get-NetTCPConnection { [CmdletBinding()] param([int]$LocalPort,[string]$State) [pscustomobject]@{OwningProcess=42424} }',
  'function Get-Process { [CmdletBinding()] param([int]$Id) [pscustomobject]@{StartTime=$global:order03FixtureStart;MainModule=[pscustomobject]@{FileName=$env:ORDER03_VERIFIED_EXE}} }',
  'function Get-CimInstance { [CmdletBinding()] param([string]$ClassName,[string]$Filter) $global:order03CimCount++; [pscustomobject]@{CreationDate=$global:order03FixtureStart.AddTicks(-2);ExecutablePath=$env:ORDER03_VERIFIED_EXE;CommandLine=(' + "'\"'" + '+$env:ORDER03_VERIFIED_EXE+' + "'\" -resource-set \"'" + '+$env:ORDER03_MANIFEST+' + "'\" -platform-content \"'" + '+(Join-Path $env:ORDER03_OUTPUT "ios.json")+' + "'\" -cn-save-path C:\\private\\save.json'" + ')} }',
  '& $env:ORDER03_SCRIPT -TestRoot $env:ORDER03_OUTPUT -Port 27040',
  '$env:ORDER03_VERIFIED_EXE=$env:ORDER03_MANIFEST; $global:order03CimCount=0',
  '& $env:ORDER03_SCRIPT -TestRoot $env:ORDER03_OUTPUT -Port 27040',
  '[IO.File]::WriteAllText((Join-Path $env:ORDER03_OUTPUT "cim-count.txt"),[string]$global:order03CimCount)'
].join('\n');
const testFile = path.join(output, 'verify.ps1');
fs.writeFileSync(testFile, command);
const run = spawnSync('powershell.exe', ['-NoProfile','-ExecutionPolicy','Bypass','-File',testFile], {
  env: { ...process.env, ORDER03_SCRIPT:source, ORDER03_MANIFEST:manifest, ORDER03_OUTPUT:output, ORDER03_VERIFIED_EXE:verifiedExe, TEMP:output, TMP:output },
  encoding:'utf8', windowsHide:true, timeout:120000
});
if (run.status !== 0) throw new Error(run.stdout + run.stderr);
const result = JSON.parse(fs.readFileSync(path.join(output, 'results.json'), 'utf8'));
assert.deepEqual(result.selected.resourceSet, ['C:\\final\\resource-set.json']);
assert.deepEqual(result.selected.platformProfiles, ['C:\\profile\\ios.json']);
assert.deepEqual(result.selected.platformConfig, []);
assert.equal(result.resources.tables.length, 29);
assert.equal(result.resources.staticResources.length, 2);
assert.equal(result.unsafeRejected, true);
assert.equal(JSON.stringify(result).includes('SYNTHETIC-SECRET'), false);
assert.equal(JSON.stringify(result).includes('PRIVATE-SEED-LOCATOR'), false);
assert.equal(JSON.stringify(result).includes('private\\save'), false);
const fullOutputs = fs.readdirSync(output).filter(name => name.startsWith('Order03-Selection-') && !name.endsWith('.zip'));
assert.equal(fullOutputs.length, 2);
const reports = fullOutputs.map(name => ({ name, report:JSON.parse(fs.readFileSync(path.join(output, name, 'selection-lyd.json'), 'utf8')) }));
const successful = reports.find(entry => entry.report.state === 'selected_paths_hashed');
assert.ok(successful);
const fullOutput = successful.name;
const collected = successful.report;
assert.equal(collected.state, 'selected_paths_hashed');
assert.equal(collected.sameObservedProcess, true);
assert.equal(collected.selectedResources.tables.length, 29);
assert.equal(collected.platformProfiles[0].platform, 'iOS');
assert.equal(JSON.stringify(collected).includes('private\\save'), false);
assert.ok(fs.existsSync(path.join(output, fullOutput + '.zip')));
assert.equal(reports.find(entry => entry.report.state === 'incomplete').report.failureStage, 'executable_identity');
assert.equal(fs.readFileSync(path.join(output, 'cim-count.txt'), 'utf8'), '0');
const report = {
  checkedAt:new Date().toISOString(), success:true, platform:'Windows PowerShell 5.1',
  scriptSha256:crypto.createHash('sha256').update(fs.readFileSync(source)).digest('hex'),
  checks:['Windows quoted argument parser','last-value resource flag selection','profile flag handling','ignored private save-path arguments','streamed locator-only JSON deserialization','29 separated table files','static runtime file identities','traversal rejection','sensitive unrelated config fields excluded','normal collector entry point with synthetic process observations','CIM timestamp precision tolerance','unverified executable rejected before reading process arguments','ZIP generated'],
  serverExecuted:false, scope:'Synthetic local fixtures and parser/read helpers. Actual server invocation still requires the user to execute the collector.'
};
fs.writeFileSync(path.join(root, 'evidence/ios-package-search/order03-runtime-selection-tests-lyd.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report,null,2));
