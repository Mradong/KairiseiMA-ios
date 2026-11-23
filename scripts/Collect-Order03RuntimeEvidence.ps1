param(
    [Parameter(Mandatory=$true)][string]$TablesRoot,
    [Parameter(Mandatory=$true)][string]$ExpectedTables,
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$CardTablesRoot,
    [string]$ServerBinary,
    [int]$ServerProcessId = 0,
    [string]$ResourceManifest,
    [string]$QuestRuntime
)
$ErrorActionPreference = 'Stop'

# Reads only explicitly selected files and one explicitly selected process.
# Never reads command lines, environment, config, accounts, memory or API sessions.
function Get-PlainFile([string]$Path, [long]$MaximumBytes) {
    $file = Get-Item -LiteralPath $Path -Force
    if ($file.PSIsContainer -or ($file.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Expected an ordinary file.' }
    if ($file.Length -gt $MaximumBytes) { throw 'Selected file exceeds the capture limit.' }
    $ancestor = $file.Directory
    while ($ancestor) {
        if ($ancestor.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse-point parents are not accepted.' }
        $ancestor = $ancestor.Parent
    }
    return $file
}
function Get-Identity([string]$Path, [long]$MaximumBytes = 134217728) {
    $before = Get-PlainFile $Path $MaximumBytes
    $bytes = $before.Length
    $write = $before.LastWriteTimeUtc
    $digest = (Get-FileHash -LiteralPath $before.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $after = Get-PlainFile $Path $MaximumBytes
    if ($after.Length -ne $bytes -or $after.LastWriteTimeUtc -ne $write) { throw 'Selected file changed while being hashed.' }
    return [ordered]@{ bytes=$bytes; sha256=$digest; lastWriteUtc=$write.ToString('o') }
}
function Get-ProcessObservation([int]$SelectedProcess, [string]$SelectedBinary) {
    if ($SelectedProcess -le 0) { return [ordered]@{ state='not_requested'; imageMatchesSelectedBinary=$null } }
    try {
        $process = Get-Process -Id $SelectedProcess -ErrorAction Stop
        $start = $process.StartTime.ToUniversalTime().ToString('o')
        $image = $process.MainModule.FileName
        $matches = if ($SelectedBinary) { [string]::Equals([IO.Path]::GetFullPath($image), $SelectedBinary, [StringComparison]::OrdinalIgnoreCase) } else { $null }
        return [ordered]@{ state='observed'; startUtc=$start; imageMatchesSelectedBinary=$matches }
    } catch { return [ordered]@{ state='unavailable_or_access_denied'; imageMatchesSelectedBinary=$null } }
}

$projectRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$projectPrefix = $projectRoot.TrimEnd('\') + '\'
$outputPath = [IO.Path]::GetFullPath($Output)
if (!$outputPath.StartsWith($projectPrefix, [StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetExtension($outputPath) -ne '.json') { throw 'Output must be a new JSON file inside this handoff/project directory.' }
if (Test-Path -LiteralPath $outputPath) { throw 'Output already exists; select a new evidence filename.' }
$outputAncestor = Split-Path -Parent $outputPath
while ($outputAncestor) {
    if (Test-Path -LiteralPath $outputAncestor) {
        $directory = Get-Item -LiteralPath $outputAncestor -Force
        if (!$directory.PSIsContainer -or ($directory.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Output parents must be ordinary directories.' }
    }
    $nextAncestor = Split-Path -Parent $outputAncestor
    if ($nextAncestor -eq $outputAncestor) { break }
    $outputAncestor = $nextAncestor
}
$expectedFile = Get-PlainFile $ExpectedTables 65536
$expectedBytes = [IO.File]::ReadAllBytes($expectedFile.FullName)
$expected = [Text.Encoding]::UTF8.GetString($expectedBytes).TrimStart([char]0xfeff) | ConvertFrom-Json
if ($expected.schemaVersion -ne 1 -or @($expected.tables).Count -ne 29) { throw 'Expected the reviewed 29-table hash manifest.' }
$allowed = @('card.csv','buddy.csv','sphr.csv','burst_gauge.csv','burst_gauge_reaction_rate.csv','skill_player.csv','skill_role_player.csv','burst_skill.csv','burst_skill_role.csv','support_skill.csv','support_skill_lvup.csv','support_skill_role.csv','skill_role_param_rule.csv','enemy_party.csv','enemy.csv','enemy_lvup.csv','enemy_ai_order.csv','skill_enemy.csv','skill_role_enemy.csv','enemy_trance_gauge_reaction.csv','skill_player_alchemist.csv','skill_role_player_alchemist.csv','tower_skill_enemy.csv','tower_skill_role_enemy.csv','tower_enemy_ai_order.csv','card_alchemist.csv','summons.csv','npc_ai.csv','npc_ai_situation_param_rule.csv')
if (@(Compare-Object ($allowed | Sort-Object) (@($expected.tables | ForEach-Object { $_.name }) | Sort-Object)).Count -ne 0) { throw 'Table names differ from the explicit allowlist.' }
foreach ($entry in $expected.tables) {
    if ($entry.sha256 -notmatch '^[0-9a-f]{64}$' -or $entry.bytes -lt 0 -or $entry.bytes -gt 67108864) { throw 'Invalid expected table identity.' }
}
$tableDirectory = Get-Item -LiteralPath $TablesRoot -Force
if (!$tableDirectory.PSIsContainer -or ($tableDirectory.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Expected an ordinary table directory.' }
$cardDirectory = if ($CardTablesRoot) { Get-Item -LiteralPath $CardTablesRoot -Force } else { $tableDirectory }
if (!$cardDirectory.PSIsContainer -or ($cardDirectory.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Expected an ordinary card table directory.' }
$cardNames = @('card.csv','buddy.csv','card_alchemist.csv')
$binaryPath = if ($ServerBinary) { (Get-PlainFile $ServerBinary 268435456).FullName } else { $null }
$started = [DateTime]::UtcNow.ToString('o')
$processBefore = Get-ProcessObservation $ServerProcessId $binaryPath
$binary = if ($binaryPath) { Get-Identity $binaryPath 268435456 } else { $null }
$tables = @()
foreach ($entry in $expected.tables) {
    $directory = if ($cardNames -contains $entry.name) { $cardDirectory } else { $tableDirectory }
    $file = Join-Path $directory.FullName $entry.name
    if (!(Test-Path -LiteralPath $file -PathType Leaf)) {
        $tables += [ordered]@{ name=$entry.name; state='missing'; matchesExpected=$false }
        continue
    }
    $identity = Get-Identity $file 67108864
    $tables += [ordered]@{ name=$entry.name; state='hashed'; bytes=$identity.bytes; sha256=$identity.sha256; lastWriteUtc=$identity.lastWriteUtc; matchesExpected=($identity.bytes -eq $entry.bytes -and $identity.sha256 -eq $entry.sha256) }
}
$resources = @()
foreach ($selection in @(@{ label='resource_manifest'; path=$ResourceManifest }, @{ label='quest_runtime'; path=$QuestRuntime })) {
    if ($selection.path) { $resources += [ordered]@{ role=$selection.label; state='hashed_only'; identity=(Get-Identity $selection.path) } }
    else { $resources += [ordered]@{ role=$selection.label; state='not_requested'; identity=$null } }
}
# Re-hash every observed file to detect changes across the collection interval.
$stable = $true
foreach ($entry in $tables) {
    if ($entry.state -ne 'hashed') { continue }
    $directory = if ($cardNames -contains $entry.name) { $cardDirectory } else { $tableDirectory }
    $again = Get-Identity (Join-Path $directory.FullName $entry.name) 67108864
    if ($again.sha256 -ne $entry.sha256 -or $again.bytes -ne $entry.bytes -or $again.lastWriteUtc -ne $entry.lastWriteUtc) { $stable = $false }
}
$binaryStable = if ($binaryPath) { $again = Get-Identity $binaryPath 268435456; $again.sha256 -eq $binary.sha256 -and $again.bytes -eq $binary.bytes -and $again.lastWriteUtc -eq $binary.lastWriteUtc } else { $null }
$processAfter = Get-ProcessObservation $ServerProcessId $binaryPath
$sameProcess = if ($ServerProcessId -gt 0) { $processBefore.state -eq 'observed' -and $processAfter.state -eq 'observed' -and $processBefore.startUtc -eq $processAfter.startUtc -and $processBefore.imageMatchesSelectedBinary -eq $processAfter.imageMatchesSelectedBinary } else { $null }
$report = [ordered]@{
    schemaVersion=1; collectionStartedUtc=$started; collectionFinishedUtc=[DateTime]::UtcNow.ToString('o')
    expectedManifestSha256=([BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash($expectedBytes))).Replace('-','').ToLowerInvariant()
    matchedTables=@($tables | Where-Object { $_.matchesExpected }).Count; totalTables=$tables.Count
    diskTablesStableDuringCollection=$stable; binaryStableDuringCollection=$binaryStable
    stabilityScope='Two hash/metadata observations around collection, not continuous monitoring or proof of in-memory loaded bytes.'
    processBefore=$processBefore; processAfter=$processAfter; sameObservedProcessDuringCollection=$sameProcess
    selectedBinary=$binary; tables=$tables; otherDiskFiles=$resources
    effectiveLoadedContent='UNKNOWN: disk hashes and process image identity do not inspect loaded content or prove which configuration selected these files.'
    deployedSourceVersion='UNKNOWN: supply matching build/source and startup or versioned runtime evidence separately.'
    sourceClientAndConversion='UNKNOWN: this collector cannot establish historical client/export provenance.'
    battleContract='NOT_TESTED: no business request, start, continue, end, HomeShow or account access.'
    privacy='Only allowlisted table filenames, hashes, sizes, times and selected-process observations; no absolute paths, command lines, environment, credentials, accounts, raw configuration or memory.'
}
$parent = Split-Path -Parent $outputPath
if (!(Test-Path -LiteralPath $parent -PathType Container)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
$outputBytes = [Text.UTF8Encoding]::new($false).GetBytes(($report | ConvertTo-Json -Depth 9) + "`n")
$stream = [IO.File]::Open($outputPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
try { $stream.Write($outputBytes, 0, $outputBytes.Length) } finally { $stream.Dispose() }
Write-Output ('Collected {0}/{1} matching disk tables; effective loaded version remains UNKNOWN.' -f $report.matchedTables,$report.totalTables)
