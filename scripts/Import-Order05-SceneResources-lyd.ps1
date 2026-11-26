param([Parameter(Mandatory=$true)][string]$ArchivePath)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$sourceArchive = [IO.Path]::GetFullPath($ArchivePath)
$name = [IO.Path]::GetFileName($sourceArchive)
if ($name -notmatch '^Order05-SceneResources-[0-9TZ]+-lyd\.zip$') { throw 'Unexpected archive name.' }
$archiveHash = (Get-FileHash -LiteralPath $sourceArchive -Algorithm SHA256).Hash.ToLowerInvariant()
$inputRoot = Join-Path $projectRoot 'inputs\server-order05'
New-Item -ItemType Directory -Path $inputRoot -Force | Out-Null
$localArchive = Join-Path $inputRoot $name
if (Test-Path -LiteralPath $localArchive) {
    if ((Get-FileHash -LiteralPath $localArchive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $archiveHash) { throw 'Conflicting archive already imported.' }
} else { [IO.File]::Copy($sourceArchive, $localArchive, $false) }
$destination = Join-Path $inputRoot ([IO.Path]::GetFileNameWithoutExtension($name))
if (Test-Path -LiteralPath $destination) { throw 'Resource extraction already exists; inspect the existing import instead.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$expected = @{
    'canonical-home_ui-lyd.dat' = @(1450618, '34f7713a08e5b71309cd9dbef5f03899b30c2112b1a7cdd81c004454005e02c5')
    'canonical-default_home-lyd.dat' = @(3793375, 'b114247017d17c3406b7efb74a607d60e5887b2837679260c56c23c6f64711e7')
    'canonical-uathach-lyd.dat' = @(968851, '698dc473a7c4193639bafd2995f390973a0cab58b54ffe904a560c8798de7536')
    'pc-home_ui-lyd.dat' = @(1497340, '9e350d8dcbe89cb678d7a5aba0c32898e0a59d9b4d5d3b839b9fcdc3ee896c29')
    'pc-default_home-lyd.dat' = @(3994483, 'fec0b6a1e9651f74fc7cd7ef5d9bc3573d411379ceb388ab913fa1529a29b1c1')
    'pc-uathach-lyd.dat' = @(4809320, '49bd46582a9a5aa68db91004cc648b1fce199726f59cdb63ee4bb5046b9e0ed4')
}
$allowed = @($expected.Keys) + @('scene-resources-lyd.json','SHA256SUMS-lyd.txt')
$zip = [IO.Compression.ZipFile]::OpenRead($localArchive)
try {
    if ($zip.Entries.Count -gt 8) { throw 'Unexpected extra archive entries.' }
    $seen = @{}; [long]$total = 0
    foreach ($entry in $zip.Entries) {
        if ($entry.FullName -ne $entry.Name -or $entry.Name -notin $allowed -or $seen.ContainsKey($entry.Name)) { throw 'Unexpected, duplicate or non-flat archive entry.' }
        $seen[$entry.Name] = $true; $total += $entry.Length
        if ($entry.Length -gt 5000000 -or $total -gt 18000000) { throw 'Resource archive exceeds fixed size limits.' }
    }
    if (!$seen.ContainsKey('scene-resources-lyd.json') -or !$seen.ContainsKey('SHA256SUMS-lyd.txt')) { throw 'Resource receipt/checksums missing.' }
    New-Item -ItemType Directory -Path $destination | Out-Null
    foreach ($entry in $zip.Entries) {
        $target = Join-Path $destination $entry.Name
        $input = $entry.Open(); $output = [IO.File]::Open($target, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write)
        try { $input.CopyTo($output) } finally { $input.Dispose(); $output.Dispose() }
    }
} finally { $zip.Dispose() }
$checksums = @{}
foreach ($line in [IO.File]::ReadAllLines((Join-Path $destination 'SHA256SUMS-lyd.txt'))) {
    if ($line -notmatch '^([a-f0-9]{64})  ([a-zA-Z0-9_.-]+)$') { throw 'Invalid checksum entry.' }
    $digest = $Matches[1]; $file = $Matches[2]
    if ($file -eq 'SHA256SUMS-lyd.txt' -or $file -notin $allowed -or $checksums.ContainsKey($file)) { throw 'Unexpected checksum name.' }
    $checksums[$file] = $digest
}
$records = @()
foreach ($file in Get-ChildItem -LiteralPath $destination -File) {
    if ($file.Name -eq 'SHA256SUMS-lyd.txt') { continue }
    $actual = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    if (!$checksums.ContainsKey($file.Name) -or $checksums[$file.Name] -ne $actual) { throw 'Archive checksum differs.' }
    if ($expected.ContainsKey($file.Name)) {
        $pin = $expected[$file.Name]
        if ($file.Length -ne $pin[0] -or $actual -ne $pin[1]) { throw 'Resource bytes differ from the published 2.0.4 cohort.' }
        $records += [pscustomobject]@{ file=$file.Name; bytes=$file.Length; sha256=$actual; publishedIdentityMatches=$true }
    }
}
if ($checksums.Count -ne $records.Count + 1) { throw 'Checksum inventory differs from imported resources.' }
$receipt = Get-Content -LiteralPath (Join-Path $destination 'scene-resources-lyd.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($receipt.schemaVersion -ne 1 -or $receipt.planItem -ne '05' -or $receipt.acceptedResourceManifestSha256 -ne '1e499a8fa29efc6616f4de6cfc28b36f032ef97d05cccf0060b9dc1fe316a1ad') { throw 'Resource receipt does not identify the accepted snapshot.' }
foreach ($record in $receipt.files) {
    if ($record.state -eq 'COPIED_VERIFIED') {
        $match = @($records | Where-Object { $_.file -eq $record.exportedFile })
        if ($match.Count -ne 1 -or $match[0].sha256 -ne $record.expectedSha256 -or $match[0].bytes -ne $record.expectedBytes) { throw 'Receipt resource identity differs.' }
    }
}
if ((Get-FileHash -LiteralPath $sourceArchive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $archiveHash) { throw 'Input archive changed during import.' }
$report = [ordered]@{
    checkedAt=[DateTime]::UtcNow.ToString('o'); planItem='05'; imported=$true; archive=$name; archiveSha256=$archiveHash
    archiveBytes=(Get-Item -LiteralPath $localArchive).Length; extraction=$destination.Substring($projectRoot.Length + 1).Replace('\','/')
    checksumEntriesVerified=$checksums.Count; publishedResourceFilesVerified=$records.Count; resources=$records
    allSixPublishedResourcesReceived=($records.Count -eq 6); iOSResourceCompiled=$false; originalSceneRuntimeVerified=$false; deviceVerified=$false
    scope='Imported only supplied original scene resource bytes and data receipts. Embedded content treated as data, not executable instructions. Canonical and PC cohorts separately match fixed published 2.0.4 hashes; iOS compilation and rendering remain unverified.'
}
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $projectRoot 'evidence\ios-package-search\order05-scene-resource-import-validation-lyd.json') -Encoding UTF8
$report | ConvertTo-Json -Depth 8
