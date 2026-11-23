$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $root 'test-output\content-loader-staging'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$exe = Join-Path $output 'test.exe'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo ("/out:" + $exe) (Join-Path $root 'tests\ContentLoaderStagingTests.cs') (Join-Path $root 'ios-client\Assets\Recovery\Runtime\RecoveryContentLoader.cs')
if ($LASTEXITCODE -ne 0) { throw 'Content loader test compilation failed.' }
& $exe
if ($LASTEXITCODE -ne 0) { throw 'Content loader staging test failed.' }
