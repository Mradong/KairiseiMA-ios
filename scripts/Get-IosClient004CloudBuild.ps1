param([Parameter(Mandatory=$true)][long]$RunId,[switch]$Wait)
$ErrorActionPreference='Stop'
$recoveryRoot=Split-Path -Parent $PSScriptRoot
$gh=Join-Path $recoveryRoot 'tools\github-cli\v2.105.0\bin\gh.exe'
$node=Join-Path $recoveryRoot 'tools\node-v24.22.3-win-x64\node.exe'
$env:GH_CONFIG_DIR=Join-Path $recoveryRoot 'tools\github-auth'
$env:GH_PROMPT_DISABLED='1'
$request=Get-Content -LiteralPath (Join-Path $recoveryRoot 'evidence\ios-package-search\ios-client004-cloud-request.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$previous=''
do {
    $stateJson=& $gh run view $RunId --repo Mradong/KairiseiMA-ios --json databaseId,headSha,status,conclusion,url,jobs
    if ($LASTEXITCODE -ne 0) { throw 'Cannot read build status.' }
    $state=$stateJson | ConvertFrom-Json
    if ($state.headSha -ne $request.commit) { throw 'Build source differs from the requested recovery commit.' }
    $stateJson | Set-Content -LiteralPath (Join-Path $recoveryRoot 'evidence\ios-package-search\ios-client004-cloud-status.json') -Encoding UTF8
    $active=@($state.jobs | ForEach-Object { $_.steps | Where-Object status -eq 'in_progress' | ForEach-Object name }) -join ', '
    $label=$state.status + ' / ' + $state.conclusion + ' / ' + $active
    if ($label -ne $previous) { Write-Output $label; $previous=$label }
    if ($state.status -eq 'completed' -or !$Wait) { break }
    Start-Sleep -Seconds 15
} while ($true)
if ($state.status -ne 'completed') { Write-Output $state.url; Write-Output 'Still building. Run this script with -Wait to monitor and download on success.'; return }
if ($state.conclusion -ne 'success') {
    $failure=& $gh run view $RunId --repo Mradong/KairiseiMA-ios --log-failed
    $failure | Set-Content -LiteralPath (Join-Path $recoveryRoot ('logs\ios-client004-build-'+$RunId+'-failed.log')) -Encoding UTF8
    $failure | Select-String -Pattern 'error CS|error:|Error:|Exception|FAIL|failed' | Select-Object -Last 25 | ForEach-Object { $_.Line }
    throw 'Cloud build did not succeed; failure log is saved for repair.'
}
$existingReport=Join-Path $recoveryRoot 'evidence\ios-package-search\ipa-client004-delivery.json'
if (Test-Path -LiteralPath $existingReport) {
    $existing=Get-Content -LiteralPath $existingReport -Raw -Encoding UTF8 | ConvertFrom-Json
    $copies=@((Join-Path $recoveryRoot 'dist\ready\KairiseiMA-ios-resource-bootstrap-0.0.4-unsigned.ipa'),(Join-Path $recoveryRoot 'install-kit\KairiseiMA-ios-resource-bootstrap-0.0.4-unsigned.ipa'))
    if ($existing.runId -ne $RunId -or $existing.version -cne '0.0.4' -or $existing.sha256 -notmatch '^[a-f0-9]{64}$') { throw 'Existing delivery report does not match this build.' }
    foreach ($copy in $copies) {
        if (!(Test-Path -LiteralPath $copy) -or (Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash.ToLowerInvariant() -cne $existing.sha256) { throw 'Existing IPA copy is missing or differs from its verified report.' }
    }
    Write-Output ('Previously verified 0.0.4 IPA: ' + $existing.sha256)
    return
}
$artifact=Join-Path $recoveryRoot ('dist\cloud-artifacts\ios-client004-'+$RunId)
$ipaDirectory=Join-Path $artifact 'ipa'
$resourceDirectory=Join-Path $artifact 'resources'
New-Item -ItemType Directory -Path $artifact -Force | Out-Null
if (!(Test-Path -LiteralPath $ipaDirectory)) {
    & $gh run download $RunId --repo Mradong/KairiseiMA-ios --name ios-connection-test-unsigned --dir $ipaDirectory
    if ($LASTEXITCODE -ne 0) { throw 'IPA artifact download failed.' }
}
if (!(Test-Path -LiteralPath $resourceDirectory)) {
    & $gh run download $RunId --repo Mradong/KairiseiMA-ios --name ios-resource-delivery --dir $resourceDirectory
    if ($LASTEXITCODE -ne 0) { throw 'Resource artifact download failed.' }
}
$resourceReport=& $node (Join-Path $PSScriptRoot 'verify-ios-resource-delivery.mjs') $resourceDirectory
if ($LASTEXITCODE -ne 0) { throw 'Downloaded iOS resource artifact failed verification.' }
$resourceReport | Set-Content -LiteralPath (Join-Path $recoveryRoot 'evidence\ios-package-search\ios-client004-artifact-verification.json') -Encoding UTF8
Write-Output $resourceReport
& (Join-Path $PSScriptRoot 'Verify-IosClient004IPA.ps1') -ArtifactDirectory $ipaDirectory -RunId $RunId
if ($LASTEXITCODE -ne 0) { throw 'Downloaded IPA failed verification.' }
