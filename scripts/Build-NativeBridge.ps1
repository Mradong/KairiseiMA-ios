param()
$ErrorActionPreference = 'Stop'
$recoveryRoot = Split-Path -Parent $PSScriptRoot
$toolsRoot = Join-Path $recoveryRoot 'tools'
$goPath = Join-Path $toolsRoot 'go\bin\go.exe'
$compilerPath = Join-Path $toolsRoot 'w64devkit\bin\gcc.exe'
if (!(Test-Path -LiteralPath $goPath) -or !(Test-Path -LiteralPath $compilerPath)) { throw 'Project-local Go or C compiler is missing.' }
$buildRoot = Join-Path $recoveryRoot 'native\build\windows-amd64'
New-Item -ItemType Directory -Path $buildRoot -Force | Out-Null
$env:GOROOT = Join-Path $toolsRoot 'go'
$env:GOPATH = Join-Path $toolsRoot 'go-work'
$env:GOMODCACHE = Join-Path $toolsRoot 'go-module-cache'
$env:GOCACHE = Join-Path $toolsRoot 'go-build-cache'
$env:GOTOOLCHAIN = 'local'
$env:CC = $compilerPath
$env:CGO_ENABLED = '1'
$env:GOOS = 'windows'
$env:GOARCH = 'amd64'
$env:PATH = (Split-Path -Parent $compilerPath) + ';' + $env:PATH
$bridgeSource = Join-Path $recoveryRoot 'native\bridge'
Push-Location -LiteralPath $bridgeSource
try {
    & $goPath fmt ./...
    if ($LASTEXITCODE -ne 0) { throw 'Go formatting failed.' }
    & $goPath build -mod=mod -buildvcs=false -trimpath -buildmode=c-shared -o (Join-Path $buildRoot 'kairisei_battlecore_rebuilt.dll') .
    if ($LASTEXITCODE -ne 0) { throw 'Native bridge build failed.' }
} finally { Pop-Location }
Write-Output (Join-Path $buildRoot 'kairisei_battlecore_rebuilt.dll')
Write-Output 'Reconstructed parameters, registration and standalone battle bridge. Registered-client decoding and iOS device validation remain incomplete; no original DLL was replaced.'
