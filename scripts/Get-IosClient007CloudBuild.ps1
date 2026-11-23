param([Parameter(Mandatory=$true)][long]$RunId,[switch]$Wait)
$ErrorActionPreference='Stop'
$recoveryRoot=Split-Path -Parent $PSScriptRoot
$gh=Join-Path $recoveryRoot 'tools\github-cli\v2.105.0\bin\gh.exe'
$node=Join-Path $recoveryRoot 'tools\node-v24.22.3-win-x64\node.exe'
$env:GH_CONFIG_DIR=Join-Path $recoveryRoot 'tools\github-auth'
$env:GH_PROMPT_DISABLED='1'
$request=Get-Content -LiteralPath (Join-Path $recoveryRoot 'evidence\ios-package-search\ios-client007-cloud-request.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$previous=''
do {
    $stateJson=& $gh run view $RunId --repo Mradong/KairiseiMA-ios --json databaseId,headSha,status,conclusion,url,jobs
    if ($LASTEXITCODE -ne 0) { throw 'Cannot read build status.' }
    $state=$stateJson | ConvertFrom-Json
    if ($state.headSha -ne $request.commit) { throw 'Build source differs from the requested recovery commit.' }
    $stateJson | Set-Content -LiteralPath (Join-Path $recoveryRoot 'evidence\ios-package-search\ios-client007-cloud-status.json') -Encoding UTF8
    $active=@($state.jobs | ForEach-Object { $_.steps | Where-Object status -eq 'in_progress' | ForEach-Object name }) -join ', '
    $label=$state.status + ' / ' + $state.conclusion + ' / ' + $active
    if ($label -ne $previous) { Write-Output $label; $previous=$label }
    if ($state.status -eq 'completed' -or !$Wait) { break }
    Start-Sleep -Seconds 15
} while ($true)
if ($state.status -ne 'completed') { Write-Output $state.url; Write-Output 'Still building. Run this script with -Wait to monitor and download on success.'; return }
if ($state.conclusion -ne 'success') {
    $failure=& $gh run view $RunId --repo Mradong/KairiseiMA-ios --log-failed
    $failure | Set-Content -LiteralPath (Join-Path $recoveryRoot ('logs\ios-client007-build-'+$RunId+'-failed.log')) -Encoding UTF8
    $failure | Select-String -Pattern 'error CS|error:|Error:|Exception|FAIL|failed' | Select-Object -Last 25 | ForEach-Object { $_.Line }
    throw 'Cloud build did not succeed; failure log is saved for repair.'
}
$existingReport=Join-Path $recoveryRoot 'evidence\ios-package-search\ipa-client007-delivery.json'
if (Test-Path -LiteralPath $existingReport) {
    $existing=Get-Content -LiteralPath $existingReport -Raw -Encoding UTF8 | ConvertFrom-Json
    $copies=@((Join-Path $recoveryRoot 'dist\ready\KairiseiMA-ios-resource-bootstrap-0.0.7-unsigned.ipa'),(Join-Path $recoveryRoot 'install-kit\KairiseiMA-ios-resource-bootstrap-0.0.7-unsigned.ipa'))
    if ($existing.runId -ne $RunId -or $existing.version -cne '0.0.7' -or $existing.sha256 -notmatch '^[a-f0-9]{64}$') { throw 'Existing delivery report does not match this build.' }
    foreach ($copy in $copies) {
        if (!(Test-Path -LiteralPath $copy) -or (Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash.ToLowerInvariant() -cne $existing.sha256) { throw 'Existing IPA copy is missing or differs from its verified report.' }
    }
    Write-Output ('Previously verified 0.0.7 IPA: ' + $existing.sha256)
    return
}
$artifact=Join-Path $recoveryRoot ('dist\cloud-artifacts\ios-client007-'+$RunId)
$ipaDirectory=Join-Path $artifact 'ipa'
$resourceDirectory=Join-Path $artifact 'resources'
New-Item -ItemType Directory -Path $artifact -Force | Out-Null
function Download-ArtifactOnce([string]$name,[string]$destination) {
    if (Test-Path -LiteralPath $destination) { return }
    # A failed download must never leave a directory that a retry treats as complete.
    $staging = Join-Path $artifact ('download-' + [guid]::NewGuid().ToString('N'))
    & $gh run download $RunId --repo Mradong/KairiseiMA-ios --name $name --dir $staging
    if ($LASTEXITCODE -ne 0) { throw ('Artifact download failed; incomplete files retained at ' + $staging) }
    if (!(Test-Path -LiteralPath $staging -PathType Container)) { throw 'Artifact download did not produce a directory.' }
    Move-Item -LiteralPath $staging -Destination $destination
}
Download-ArtifactOnce 'ios-connection-test-unsigned' $ipaDirectory
Download-ArtifactOnce 'ios-resource-delivery' $resourceDirectory
$resourceReport=& $node (Join-Path $PSScriptRoot 'verify-ios-resource-delivery.mjs') $resourceDirectory
if ($LASTEXITCODE -ne 0) { throw 'Downloaded iOS resource artifact failed verification.' }
$resourceReport | Set-Content -LiteralPath (Join-Path $recoveryRoot 'evidence\ios-package-search\ios-client007-artifact-verification.json') -Encoding UTF8
Write-Output $resourceReport
& (Join-Path $PSScriptRoot 'Verify-IosClient007IPA.ps1') -ArtifactDirectory $ipaDirectory -RunId $RunId
if ($LASTEXITCODE -ne 0) { throw 'Downloaded IPA failed verification.' }
