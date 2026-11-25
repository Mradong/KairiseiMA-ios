param([string]$TestRoot=$PSScriptRoot)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Collect-Order03RuntimeSelection.ps1') -TestRoot $TestRoot -Port 27040

function Sync-Order03StartupPin([string]$Root) {
    $directory=(Get-Item -LiteralPath $Root -ErrorAction Stop).FullName
    $binary=Join-Path $directory 'bin\kairi-server.exe'
    $startup=Join-Path $directory 'Start-IOS-Test.ps1'
    $configFile=Join-Path $directory 'isolated-config.json'
    $binaryIdentity=Get-SelectionIdentity $binary
    $oldHash='7704963e90c587aa4e8167fc7e74299f231e8b22f45a10012fbbe44301727918'
    $newHash='1c1488558e2ed859cb8a9f36b8720b125c0dbceb3ac3c83e3bec2603e1fdf2c1'
    if(!$binaryIdentity.stable -or $binaryIdentity.sha256 -notin @($oldHash,$newHash)){throw 'Only the reviewed original or durable-solo program may update the startup pin.'}
    $startupIdentity=Get-SelectionIdentity $startup
    if(!$startupIdentity.stable -or $startupIdentity.sha256 -ne '9aef0e3c2ed167d476b4fc9b6135a3ae26229a574ba19ab8d82440ebd48ce6a6'){throw 'Startup checker differs from the received and reviewed script.'}
    $configIdentity=Get-SelectionIdentity $configFile
    if(!$configIdentity.stable -or $configIdentity.bytes -gt 1048576){throw 'Startup configuration is changing or too large.'}
    $bytes=[IO.File]::ReadAllBytes($configFile)
    $utf8=[Text.UTF8Encoding]::new($false,$true)
    $text=$utf8.GetString($bytes)
    $config=$text.TrimStart([char]0xfeff) | ConvertFrom-Json
    if($config.schema -ne 1 -or $config.http_port -ne 27040 -or $config.battle_port -ne 27041 -or $config.admin_port -ne 0){throw 'Expected the separate iOS test service configuration.'}
    $productionRoot=[IO.Path]::GetFullPath([string]$config.production_root).TrimEnd('\')
    $testDirectory=[IO.Path]::GetFullPath($directory).TrimEnd('\')
    if($testDirectory.Equals($productionRoot,[StringComparison]::OrdinalIgnoreCase) -or $testDirectory.StartsWith($productionRoot+'\',[StringComparison]::OrdinalIgnoreCase) -or $productionRoot.StartsWith($testDirectory+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Production and iOS test directories must remain separate.'}
    $pattern=[regex]'"expected_test_binary_sha256"\s*:\s*"([a-f0-9]{64})"'
    $pinMatches=$pattern.Matches($text)
    if($pinMatches.Count -ne 1 -or $pinMatches[0].Groups[1].Value -cne [string]$config.expected_test_binary_sha256 -or $config.expected_test_binary_sha256 -notin @($oldHash,$newHash)){throw 'The one expected test-program pin is absent, duplicated or unreviewed.'}
    $beforePin=[string]$config.expected_test_binary_sha256
    $afterPin=$binaryIdentity.sha256
    $expectedConfigHash=$configIdentity.sha256
    $report=[ordered]@{state='already_synchronized';binary=$binaryIdentity;startupScriptSha256=$startupIdentity.sha256;configSha256Before=$configIdentity.sha256;expectedHashBefore=$beforePin;expectedHashAfter=$afterPin;changedField='expected_test_binary_sha256';configurationChanged=$false;otherConfigBytesChanged=$false;startupScriptChanged=$false;productionOrResourcesAccessed=$false}
    if($beforePin -cne $afterPin) {
        $listeners=@(Get-NetTCPConnection -LocalPort @(27040,27041) -State Listen -ErrorAction SilentlyContinue)
        if($listeners.Count -gt 0){throw 'Stop the iOS test service before updating its startup pin.'}
        $capture=$pinMatches[0].Groups[1]
        $patched=$text.Substring(0,$capture.Index)+$afterPin+$text.Substring($capture.Index+$capture.Length)
        $patchedBytes=$utf8.GetBytes($patched)
        $hasher=[Security.Cryptography.SHA256]::Create()
        try{$expectedConfigHash=([BitConverter]::ToString($hasher.ComputeHash($patchedBytes))).Replace('-','').ToLowerInvariant()}finally{$hasher.Dispose()}
        $staged=Join-Path $directory ('isolated-config-pin-staged-'+[Guid]::NewGuid().ToString('N').Substring(0,8)+'-lyd.json')
        try {
            [IO.File]::WriteAllBytes($staged,$patchedBytes)
            if((Get-SelectionIdentity $configFile).sha256 -ne $configIdentity.sha256){throw 'Configuration changed before pin replacement.'}
            [IO.File]::Replace($staged,$configFile,[System.Management.Automation.Language.NullString]::Value,$true)
        } finally {
            if(Test-Path -LiteralPath $staged){Remove-Item -LiteralPath $staged -Force}
        }
        $report.configurationChanged=$true;$report.state='pin_synchronized'
    }
    $after=Get-SelectionIdentity $configFile
    $report.configSha256After=$after.sha256
    if(!$after.stable -or $after.sha256 -ne $expectedConfigHash){throw 'Configuration changed after pin update.'}
    return $report
}

if($MyInvocation.InvocationName -eq '.'){return}
$directory=(Get-Item -LiteralPath $TestRoot -ErrorAction Stop).FullName
$receipt=Join-Path $directory ('Order03-Startup-Pin-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')+'-lyd.json')
$report=[ordered]@{schemaVersion=1;collector='order03-startup-pin-20261125-1';checkedUtc=[DateTime]::UtcNow.ToString('o');success=$false}
try {
    $report.update=Sync-Order03StartupPin $directory
    $report.success=$true
    Write-Host ('State: '+$report.update.state)
    Write-Host 'Start Start-Phone-Test.cmd, then Verify-Order03-Restart-lyd.cmd. Original resource and production checks remain enabled.'
} catch {
    $report.failureClass=$_.Exception.GetType().Name
    Write-Host 'Startup pin update stopped: program, startup script or configuration does not match the reviewed setup.'
} finally {
    [IO.File]::WriteAllText($receipt,(ConvertTo-Json -InputObject $report -Depth 12)+"`n",[Text.UTF8Encoding]::new($false))
    Write-Host ('Saved: '+$receipt)
}
if(!$report.success){exit 1}
