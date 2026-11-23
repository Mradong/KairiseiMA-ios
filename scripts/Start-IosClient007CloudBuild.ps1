param()
$ErrorActionPreference = 'Stop'
$recoveryRoot = Split-Path -Parent $PSScriptRoot
$ciRoot = Join-Path $recoveryRoot 'ci-upload'
$gh = Join-Path $recoveryRoot 'tools\github-cli\v2.105.0\bin\gh.exe'
$env:GH_CONFIG_DIR = Join-Path $recoveryRoot 'tools\github-auth'
$env:GH_PROMPT_DISABLED = '1'
$env:GIT_TERMINAL_PROMPT = '0'
$env:PATH = (Split-Path -Parent $gh) + ';' + $env:PATH
$gitSafe = 'safe.directory=' + $ciRoot.Replace('\','/')
$manifest = Get-Content -LiteralPath (Join-Path $ciRoot 'ci-source-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.stage -ne 'ios-resource-cache-bootstrap' -or $manifest.gameplayReady -ne $false) { throw 'Expected reviewed resource-bootstrap source.' }
$approved = @($manifest.files | ForEach-Object { $_.path }) + @('ci-source-manifest.json')
foreach ($entry in $manifest.files) {
    if ((Get-FileHash -LiteralPath (Join-Path $ciRoot $entry.path) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.sha256) { throw ('Reviewed source changed: ' + $entry.path) }
}
$dirty = & git -c $gitSafe -C $ciRoot status --porcelain
if ($LASTEXITCODE -ne 0 -or $dirty) { throw 'Commit and review the CI source before uploading.' }
$tracked = @(& git -c $gitSafe -c core.quotepath=false -C $ciRoot ls-files)
if (@(Compare-Object ($approved | Sort-Object) ($tracked | Sort-Object)).Count -ne 0) { throw 'Tracked tree differs from source whitelist.' }
$repoJson = & $gh repo view Mradong/KairiseiMA-ios --json nameWithOwner,isPrivate
if ($LASTEXITCODE -ne 0) { throw 'Cannot verify the private build repository.' }
$repo = $repoJson | ConvertFrom-Json
if ($repo.nameWithOwner -ne 'Mradong/KairiseiMA-ios' -or $repo.isPrivate -ne $true) { throw 'Wrong repository or visibility.' }
$commit = & git -c $gitSafe -C $ciRoot rev-parse HEAD
$origin = & git -c $gitSafe -C $ciRoot remote get-url origin
if ($LASTEXITCODE -ne 0 -or $origin -ne 'https://github.com/Mradong/KairiseiMA-ios.git') { throw 'Unexpected upload remote.' }
& git -c $gitSafe -c credential.helper= -c 'credential.helper=!gh auth git-credential' -c core.hooksPath=NUL -C $ciRoot push origin main
if ($LASTEXITCODE -ne 0) { throw 'Source push failed; no workflow was requested.' }
$remote = & $gh api repos/Mradong/KairiseiMA-ios/commits/main --jq .sha
if ($LASTEXITCODE -ne 0 -or $remote -ne $commit) { throw 'Remote source hash differs; no workflow was requested.' }
& $gh workflow run build-ios.yml --repo Mradong/KairiseiMA-ios --ref main
if ($LASTEXITCODE -ne 0) { throw 'Source was uploaded, but workflow dispatch failed.' }
[pscustomobject]@{ version='0.0.7'; requestedAt=[DateTime]::UtcNow.ToString('o'); repository='Mradong/KairiseiMA-ios'; commit=$commit; sourceUploaded=$true; workflowRequested=$true; buildSucceeded=$false; deviceVerified=$false } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $recoveryRoot 'evidence\ios-package-search\ios-client007-cloud-request.json') -Encoding UTF8
Write-Output ('Requested iOS resource build for ' + $commit)
& $gh run list --repo Mradong/KairiseiMA-ios --workflow build-ios.yml --limit 3 --json databaseId,headSha,status,conclusion,url
