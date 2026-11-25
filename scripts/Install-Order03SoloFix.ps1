param([string]$TestRoot=$PSScriptRoot,[string]$ExpectedPayloadSha256,[switch]$Rollback)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Collect-Order03RuntimeSelection.ps1') -TestRoot $TestRoot -Port 27040
. (Join-Path $PSScriptRoot 'Sync-Order03StartupPin.ps1') -TestRoot $TestRoot
$oldHash='7704963e90c587aa4e8167fc7e74299f231e8b22f45a10012fbbe44301727918'
$directory=(Get-Item -LiteralPath $TestRoot -ErrorAction Stop).FullName
$bin=Join-Path $directory 'bin'
$live=Join-Path $bin 'kairi-server.exe'
$stamp=[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,6)
$receipt=Join-Path $directory ('Order03-Solo-Fix-Install-'+$stamp+'-lyd.json')
$report=[ordered]@{schemaVersion=1;collector='order03-solo-fix-install-20261125-1';startedUtc=[DateTime]::UtcNow.ToString('o');success=$false;state='identity';rollbackRequested=[bool]$Rollback;serviceStartedByInstaller=$false;accountOrResourceFilesAccessed=$false}
try {
    if($ExpectedPayloadSha256 -notmatch '^[a-f0-9]{64}$'){throw 'Expected payload hash is invalid.'}
    $before=Get-SelectionIdentity $live
    $report.binaryBefore=$before
    if(!$before.stable -or $before.sha256 -notin @($oldHash,$ExpectedPayloadSha256)){throw 'Test service binary differs from the reviewed versions.'}
    $report.state='service_stop_check'
    $listeners=@(Get-NetTCPConnection -LocalPort @(27040,27041) -State Listen -ErrorAction SilentlyContinue)
    if($listeners.Count -gt 0){throw 'Stop the iOS service window first. Test port is still listening.'}
    $report.state='startup_pin_before'
    $report.startupPinBefore=Sync-Order03StartupPin $directory
    if($Rollback) {
        if($before.sha256 -eq $oldHash){$report.success=$true;$report.state='already_original'}
        else {
            $report.state='rollback_backup_identity'
            $backups=@(Get-ChildItem -LiteralPath $bin -Filter 'kairi-server-before-solo-fix-*-lyd.exe' -File | Sort-Object Name -Descending)
            if($backups.Count -eq 0){throw 'No original-program backup was found.'}
            $backup=$backups[0]
            $backupIdentity=Get-SelectionIdentity $backup.FullName
            if(!$backupIdentity.stable -or $backupIdentity.sha256 -ne $oldHash){throw 'Original-program backup identity is invalid.'}
            $retired=Join-Path $bin ('kairi-server-rolled-back-solo-fix-'+$stamp+'-lyd.exe')
            [IO.File]::Replace($backup.FullName,$live,$retired,$true)
            $after=Get-SelectionIdentity $live
            if(!$after.stable -or $after.sha256 -ne $oldHash){throw 'Rollback program identity is invalid.'}
            $report.binaryAfter=$after;$report.replacedProgramFile=[IO.Path]::GetFileName($retired)
            $report.success=$true;$report.state='rolled_back'
        }
    } elseif($before.sha256 -eq $ExpectedPayloadSha256) {
        $report.binaryAfter=$before;$report.success=$true;$report.state='already_installed'
    } else {
        $report.state='payload_identity'
        $payload=Join-Path $PSScriptRoot 'kairi-server-durable-solo-lyd.exe'
        $payloadIdentity=Get-SelectionIdentity $payload
        if(!$payloadIdentity.stable -or $payloadIdentity.sha256 -ne $ExpectedPayloadSha256){throw 'Fix payload identity is invalid.'}
        $staged=Join-Path $bin ('kairi-server-solo-staged-'+$stamp+'-lyd.exe')
        [IO.File]::Copy($payload,$staged,$false)
        $stagedIdentity=Get-SelectionIdentity $staged
        if(!$stagedIdentity.stable -or $stagedIdentity.sha256 -ne $ExpectedPayloadSha256){throw 'Staged program identity is invalid.'}
        $backup=Join-Path $bin ('kairi-server-before-solo-fix-'+$stamp+'-lyd.exe')
        $report.state='program_replacement'
        [IO.File]::Replace($staged,$live,$backup,$true)
        $after=Get-SelectionIdentity $live
        $saved=Get-SelectionIdentity $backup
        if(!$after.stable -or $after.sha256 -ne $ExpectedPayloadSha256 -or !$saved.stable -or $saved.sha256 -ne $oldHash){throw 'Installed program or backup identity is invalid.'}
        $report.binaryAfter=$after;$report.backupFile=[IO.Path]::GetFileName($backup)
        $report.success=$true;$report.state='installed'
    }
    $completedState=$report.state
    $report.state='startup_pin_after'
    $report.startupPinAfter=Sync-Order03StartupPin $directory
    $report.state=$completedState
} catch {
    $report.success=$false
    $report.failureStage=$report.state;$report.failureClass=$_.Exception.GetType().Name
    try {
        if(!$Rollback -and $before.sha256 -eq $oldHash -and $backup -and (Test-Path -LiteralPath $backup) -and (Get-SelectionIdentity $live).sha256 -eq $ExpectedPayloadSha256) {
            $saved=Get-SelectionIdentity $backup
            if($saved.sha256 -ne $oldHash){throw 'Backup identity differs during recovery.'}
            $failed=Join-Path $bin ('kairi-server-failed-solo-fix-'+$stamp+'-lyd.exe')
            [IO.File]::Replace($backup,$live,$failed,$true)
            $report.startupPinRecovered=Sync-Order03StartupPin $directory
            $report.programRecoveredToBefore=$true
        } elseif($Rollback -and $before.sha256 -eq $ExpectedPayloadSha256 -and $retired -and (Test-Path -LiteralPath $retired) -and (Get-SelectionIdentity $live).sha256 -eq $oldHash) {
            $saved=Get-SelectionIdentity $retired
            if($saved.sha256 -ne $ExpectedPayloadSha256){throw 'Retired program identity differs during rollback recovery.'}
            $recovered=Join-Path $bin ('kairi-server-rollback-recovery-'+$stamp+'-lyd.exe')
            [IO.File]::Replace($retired,$live,$recovered,$true)
            $report.startupPinRecovered=Sync-Order03StartupPin $directory
            $report.programRecoveredToBefore=$true
        }
    } catch {$report.recoveryFailureClass=$_.Exception.GetType().Name}
    Write-Host 'Install/rollback stopped. Check that the iOS test service is stopped and all package files are in IOS-Isolated-Test.'
} finally {
    $report.finishedUtc=[DateTime]::UtcNow.ToString('o')
    [IO.File]::WriteAllText($receipt,(ConvertTo-Json -InputObject $report -Depth 12)+"`n",[Text.UTF8Encoding]::new($false))
    Write-Host ('State: '+$report.state);Write-Host ('Saved: '+$receipt)
    if($report.success){Write-Host 'Now start Start-Phone-Test.cmd. For the fix, then run Verify-Order03-Restart-lyd.cmd.'}
}
if(!$report.success){exit 1}
