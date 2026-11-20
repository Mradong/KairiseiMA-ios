param(
    [Parameter(Mandatory=$true)][string]$ArtifactDirectory,
    [Parameter(Mandatory=$true)][long]$RunId
)
$ErrorActionPreference='Stop'
$recoveryRoot=[System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$artifactRoot=[System.IO.Path]::GetFullPath($ArtifactDirectory)
if (!$artifactRoot.StartsWith($recoveryRoot+[System.IO.Path]::DirectorySeparatorChar,[System.StringComparison]::OrdinalIgnoreCase)) { throw 'Artifact must remain inside the recovery directory.' }
$ipaFiles=@(Get-ChildItem -LiteralPath $artifactRoot -Filter '*.ipa' -File -Recurse)
if ($ipaFiles.Count -ne 1) { throw 'Expected exactly one downloaded IPA.' }
$ipa=$ipaFiles[0]
$reportRoot=$ipa.DirectoryName
$expectedHash=([System.IO.File]::ReadAllText((Join-Path $reportRoot 'ipa-sha256.txt')) -split '\s+')[0].ToLowerInvariant()
$actualHash=(Get-FileHash -LiteralPath $ipa.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
if ($expectedHash -notmatch '^[a-f0-9]{64}$' -or $expectedHash -cne $actualHash) { throw 'IPA differs from the cloud checksum.' }
$info=Get-Content -LiteralPath (Join-Path $reportRoot 'application-info.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($info.CFBundleIdentifier -cne 'local.kairisei.recovery' -or $info.CFBundleShortVersionString -cne '0.0.5' -or [string]$info.CFBundleVersion -cne '5') { throw 'Unexpected app identifier or native-validation version.' }
$native=Get-Content -LiteralPath (Join-Path $reportRoot 'native-build.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($native.target -cne 'ios/arm64' -or $native.go -cne '1.25.1' -or $native.dependencyCommit -cne 'd1a202eb539b99511e8a33f3975fac409ab545bf' -or $native.gameplayReady) { throw 'Unexpected native build report.' }
$extraction=[System.IO.Path]::GetFullPath((Join-Path $recoveryRoot ('test-output\ipa-client005-'+$RunId)))
if (!$extraction.StartsWith($recoveryRoot+[System.IO.Path]::DirectorySeparatorChar,[System.StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $extraction)) { throw 'Extraction requires a new directory inside the recovery workspace.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive=[System.IO.Compression.ZipFile]::OpenRead($ipa.FullName)
try {
    $totalBytes=[long]0
    $names=New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $archive.Entries) {
        $name=$entry.FullName.Replace([char]92,[char]47)
        if (!$name.StartsWith('Payload/') -or $name.Contains(':') -or $name.Contains([char]0) -or $name.Split('/') -contains '..' -or !$names.Add($name)) { throw 'Unsafe or duplicated IPA entry.' }
        $mode=([int64]$entry.ExternalAttributes -shr 16) -band 0xF000
        if ($mode -eq 0xA000) { throw 'Symlinks are not accepted in this device IPA.' }
        $totalBytes+=$entry.Length
        if ($totalBytes -gt 536870912 -or $archive.Entries.Count -gt 5000) { throw 'Unexpected IPA extraction size.' }
        $target=[System.IO.Path]::GetFullPath((Join-Path $extraction $name))
        if (!$target.StartsWith($extraction+[System.IO.Path]::DirectorySeparatorChar,[System.StringComparison]::OrdinalIgnoreCase)) { throw 'IPA path escapes extraction directory.' }
    }
    [System.IO.Directory]::CreateDirectory($extraction) | Out-Null
    foreach ($entry in $archive.Entries) {
        $name=$entry.FullName.Replace([char]92,[char]47)
        $target=Join-Path $extraction $name
        if ($name.EndsWith('/')) { [System.IO.Directory]::CreateDirectory($target) | Out-Null }
        else {
            [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($target)) | Out-Null
            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry,$target,$false)
        }
    }
    $entryCount=$archive.Entries.Count
} finally { $archive.Dispose() }
$apps=@(Get-ChildItem -LiteralPath (Join-Path $extraction 'Payload') -Directory -Filter '*.app')
if ($apps.Count -ne 1) { throw 'IPA must contain one app.' }
$validation=& (Join-Path $recoveryRoot 'tools\node-v24.22.3-win-x64\node.exe') (Join-Path $PSScriptRoot 'verify-ios-build.mjs') $apps[0].FullName (Join-Path $reportRoot 'application-info.json') '17.4.1'
if ($LASTEXITCODE -ne 0) { throw 'Local iOS structure validation failed.' }
$local=$validation | ConvertFrom-Json
$cloud=Get-Content -LiteralPath (Join-Path $reportRoot 'structural-validation.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($local.executableSHA256 -cne $cloud.executableSHA256) { throw 'Cloud/local executable hashes differ.' }
$framework=Join-Path $apps[0].FullName 'Frameworks\UnityFramework.framework\UnityFramework'
$frameworkText=[System.Text.Encoding]::ASCII.GetString([System.IO.File]::ReadAllBytes($framework))
$goMarkers=$frameworkText.Contains('go1.25.1') -and $frameworkText.Contains('kairisei.local/server/clientrebuildbridge')
if (!$goMarkers) { throw 'The linked UnityFramework lacks the expected reconstructed Go runtime markers.' }
$stage=Get-Content -LiteralPath (Join-Path $reportRoot 'recovery-build.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($stage.stage -cne 'ios-resource-cache-bootstrap' -or $stage.gameplayReady) { throw 'Unexpected resource build stage.' }
$publicManifests=@(Get-ChildItem -LiteralPath $apps[0].FullName -Recurse -File -Filter 'client-manifest.json')
if ($publicManifests.Count -ne 1) { throw 'Expected one embedded iOS content manifest.' }
$embeddedJson=& (Join-Path $recoveryRoot 'tools\node-v24.22.3-win-x64\node.exe') (Join-Path $PSScriptRoot 'verify-ios-resource-delivery.mjs') $publicManifests[0].DirectoryName
if ($LASTEXITCODE -ne 0) { throw 'Embedded iOS resource package failed hash/CRC/manifest verification.' }
$embeddedResourceValidation=$embeddedJson | ConvertFrom-Json
$ready=Join-Path $recoveryRoot 'dist\ready\KairiseiMA-ios-resource-bootstrap-0.0.5-unsigned.ipa'
$kit=Join-Path $recoveryRoot 'install-kit\KairiseiMA-ios-resource-bootstrap-0.0.5-unsigned.ipa'
foreach ($destination in @($ready,$kit)) {
    if (Test-Path -LiteralPath $destination) { throw 'Do not overwrite an existing delivered native IPA.' }
}
[System.IO.File]::Copy($ipa.FullName,$ready,$false)
[System.IO.File]::Copy($ipa.FullName,$kit,$false)
$record=[ordered]@{checkedAt=[DateTime]::UtcNow.ToString('o');runId=$RunId;stage='ios-resource-cache-bootstrap';version='0.0.5';bundleIdentifier=$info.CFBundleIdentifier;file='dist/ready/KairiseiMA-ios-resource-bootstrap-0.0.5-unsigned.ipa';bytes=$ipa.Length;sha256=$actualHash;matchesCloudChecksum=$true;ipaZipEntries=$entryCount;localStructuralValidation=$local;embeddedResourceValidation=$embeddedResourceValidation;linkedGoMarkersVerified=$goMarkers;nativeBuild=$native;signedForUsersPhone=$false;installedOnPhone=$false;deviceRuntimeTested=$false;gameplayReady=$false}
[System.IO.File]::WriteAllText((Join-Path $recoveryRoot 'evidence\ios-package-search\ipa-client005-delivery.json'),($record | ConvertTo-Json -Depth 12),[System.Text.UTF8Encoding]::new($false))
Write-Output ([pscustomobject]$record | Select-Object runId,version,file,bytes,sha256,linkedGoMarkersVerified | ConvertTo-Json)
