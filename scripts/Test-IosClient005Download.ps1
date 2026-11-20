$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$tokens=$null;$errors=$null
$tree=[System.Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Get-IosClient005CloudBuild.ps1'),[ref]$tokens,[ref]$errors)
if($errors.Count -ne 0){throw 'Download script syntax invalid.'}
$function=$tree.Find({param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Download-ArtifactOnce'},$true)
if($null -eq $function){throw 'Download function not found.'}
Invoke-Expression $function.Extent.Text
$artifact=Join-Path $root ('test-output\client005-download-test-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $artifact | Out-Null
$RunId=123;$script:mockFailure=$true;$gh='Mock-Download'
function Mock-Download {
    $target=$args[([array]::IndexOf($args,'--dir')+1)]
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    if($script:mockFailure){[IO.File]::WriteAllText((Join-Path $target 'partial'),'partial');$global:LASTEXITCODE=1}
    else {[IO.File]::WriteAllText((Join-Path $target 'complete'),'complete');$global:LASTEXITCODE=0}
}
$destination=Join-Path $artifact 'ipa'
$failed=$false
try {Download-ArtifactOnce 'example' $destination} catch {$failed=$true}
if(!$failed -or (Test-Path -LiteralPath $destination)){throw 'Failed download was treated as complete.'}
$script:mockFailure=$false
Download-ArtifactOnce 'example' $destination
if(!(Test-Path -LiteralPath (Join-Path $destination 'complete')) -or (Test-Path -LiteralPath (Join-Path $destination 'partial'))){throw 'Retry did not isolate completed download.'}
if(@(Get-ChildItem -LiteralPath $artifact -Directory -Filter 'download-*').Count -ne 1){throw 'Expected incomplete staging directory to remain separate.'}
Write-Output 'PASS: failed download preserves separate staging and retry promotes only completed artifact.'
