param()
$ErrorActionPreference = 'Stop'
$recoveryRoot = Split-Path -Parent $PSScriptRoot
$toolsRoot = Join-Path $recoveryRoot 'tools'
$env:GOROOT = Join-Path $toolsRoot 'go'
$env:GOPATH = Join-Path $toolsRoot 'go-work'
$env:GOMODCACHE = Join-Path $toolsRoot 'go-module-cache'
$env:GOCACHE = Join-Path $toolsRoot 'go-build-cache'
$env:GOTMPDIR = Join-Path $recoveryRoot 'test-output\go-tmp'
$env:GOTOOLCHAIN = 'local'
$env:CGO_ENABLED = '0'
$env:GOARCH = 'amd64'
$serverRoot = Join-Path $recoveryRoot 'server-ios\server'
$records = @()
foreach ($target in @('windows','linux')) {
    $env:GOOS = $target
    $output = Join-Path $recoveryRoot ('dist\server-ios\' + $target + '-amd64')
    New-Item -ItemType Directory -Path $output -Force | Out-Null
    $name = 'kairi-server'
    if ($target -eq 'windows') { $name += '.exe' }
    $executable = Join-Path $output $name
    & (Join-Path $toolsRoot 'go\bin\go.exe') -C $serverRoot build -buildvcs=false -trimpath -o $executable ./cmd/kairi-server
    if ($LASTEXITCODE -ne 0) { throw ('Server build failed for ' + $target) }
    $records += [pscustomobject]@{ platform=$target; architecture='amd64'; bytes=(Get-Item -LiteralPath $executable).Length; sha256=(Get-FileHash -LiteralPath $executable -Algorithm SHA256).Hash.ToLowerInvariant(); output=$executable }
}
[pscustomobject]@{ generatedAt=[DateTime]::UtcNow.ToString('o'); referenceCommit='d1a202eb539b99511e8a33f3975fac409ab545bf'; binaries=$records; deployed=$false; scope='Isolated iOS-capable server binaries; full game deployment still requires complete matching resource-set, platform content and separate test database.' } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $recoveryRoot 'evidence\ios-package-search\ios-server-build.json') -Encoding UTF8
Write-Output 'Built Windows and Linux iOS-capable server binaries; no live service was changed.'
