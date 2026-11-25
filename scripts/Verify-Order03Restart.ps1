param([string]$TestRoot=$PSScriptRoot,[int]$Port=27040,[int]$ReadyAttempts=90,[string]$ExpectedExecutableSha256='7704963e90c587aa4e8167fc7e74299f231e8b22f45a10012fbbe44301727918')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Collect-Order03RuntimeSelection.ps1') -TestRoot $TestRoot -Port $Port

function Invoke-Order03Request([string]$Route,[string]$Body,[string]$Method='POST',[int]$TimeoutMilliseconds=20000) {
    if($report){
        $report.lastRequestRoute=$Route
        foreach($key in @('lastNetworkFailureClass','lastNetworkStatus','lastHttpStatus','lastBusinessCode')){$report.Remove($key)}
    }
    $request=[Net.HttpWebRequest]::Create('http://127.0.0.1:'+$Port+$Route)
    $request.Method=$Method;$request.ContentType='application/json';$request.Timeout=$TimeoutMilliseconds;$request.ReadWriteTimeout=$TimeoutMilliseconds
    $request.Proxy=$null
    try {
        if($Method -eq 'POST') {
            $data=[Text.Encoding]::UTF8.GetBytes($Body);$request.ContentLength=$data.Length
            $stream=$request.GetRequestStream()
            try{$stream.Write($data,0,$data.Length)}finally{$stream.Dispose()}
        }
        $response=$request.GetResponse()
    } catch {
        $networkError=$_.Exception
        while($networkError){
            if($report){
                $report.lastNetworkFailureClass=$networkError.GetType().Name
                if($networkError -is [Net.WebException]){
                    $report.lastNetworkStatus=[string]$networkError.Status
                    if($networkError.Response){$report.lastHttpStatus=[int]$networkError.Response.StatusCode;$networkError.Response.Close()}
                }
            }
            $networkError=$networkError.InnerException
        }
        throw
    }
    try {
        $reader=[IO.StreamReader]::new($response.GetResponseStream(),[Text.Encoding]::UTF8)
        try{$text=$reader.ReadToEnd()}finally{$reader.Dispose()}
        return $text
    } finally {$response.Dispose()}
}
function Invoke-Order03RecoveryRequest([string]$Route,[string]$Body) {
    for($retry=0;$retry -lt 3;$retry++){
        try{return Invoke-Order03Request $Route $Body}catch{
            $retryable=$report.lastNetworkStatus -in @('ConnectFailure','ConnectionClosed','ReceiveFailure','SendFailure','Timeout') -or $report.lastHttpStatus -ge 500
            if(!$retryable -or $retry -eq 2){throw}
            $report.recoveryFailureSamples=@($report.recoveryFailureSamples)+@{route=$Route;httpStatus=$report.lastHttpStatus;networkStatus=$report.lastNetworkStatus}
            $report.recoveryRequestRetries=$retry+1
            Start-Sleep -Seconds 1
        }
    }
}
function Read-Order03Envelope([string]$Text) {
    $parts=@($Text.Trim().Split([char]10) | Where-Object {$_.Trim()})
    $common=$parts[0] | ConvertFrom-Json
    if($report){$report.lastBusinessCode=$common.res_code}
    if($common.res_code -ne 0 -or $parts.Count -lt 2){throw 'Business request failed.'}
    return $parts[1] | ConvertFrom-Json
}
function Get-Order03TestProcess {
    $owners=@(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction Stop | Select-Object -ExpandProperty OwningProcess -Unique)
    if($owners.Count -ne 1){throw 'One test listener is required.'}
    $process=Get-Process -Id $owners[0] -ErrorAction Stop
    $identity=Get-SelectionIdentity $process.MainModule.FileName
    if(!$identity.stable -or $identity.sha256 -ne $ExpectedExecutableSha256){throw 'Test executable differs from reviewed version.'}
    return [ordered]@{startUtc=$process.StartTime.ToUniversalTime().ToString('o');executable=$identity}
}
function Wait-Order03ServiceReady([string]$Phase='startup') {
    Write-Host 'Waiting for the iOS test service. Start Start-Phone-Test.cmd and keep its window open.'
    $deadline=[DateTime]::UtcNow.AddMinutes(3)
    for($attempt=0;$attempt -lt $ReadyAttempts -and [DateTime]::UtcNow -lt $deadline;$attempt++) {
        $owners=@(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique)
        if($owners.Count -gt 1){throw 'Multiple test listeners observed.'}
        if($owners.Count -eq 1) {
            $process=Get-Order03TestProcess
            try {
                $health=Invoke-Order03Request '/healthz' '' 'GET' 3000 | ConvertFrom-Json
                if($health.state -eq 'PASS') {
                    $after=Get-Order03TestProcess
                    if($after.startUtc -eq $process.startUtc) {
                        if(!$report.serviceReadiness){$report.serviceReadiness=@{}}
                        $report.serviceReadiness[$Phase]=@{healthState='PASS';attempts=$attempt+1;loopbackProxyDisabled=$true}
                        return $after
                    }
                }
            } catch {
                $report.readinessFailureSamples=@($report.readinessFailureSamples)+@{route='/healthz';httpStatus=$report.lastHttpStatus;networkStatus=$report.lastNetworkStatus}
            }
        }
        if($attempt -lt $ReadyAttempts-1){Start-Sleep -Seconds 2}
    }
    throw 'Test service did not become ready; no further login or battle entry was requested.'
}
function Get-Order03ObjectHash($Value) {
    $json=ConvertTo-Json -InputObject $Value -Depth 40 -Compress
    $hasher=[Security.Cryptography.SHA256]::Create()
    try{return ([BitConverter]::ToString($hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes($json)))).Replace('-','').ToLowerInvariant()}
    finally{$hasher.Dispose()}
}
if($MyInvocation.InvocationName -eq '.'){return}
$output=Join-Path (Get-Item -LiteralPath $TestRoot).FullName ('Order03-Restart-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))
$output += '-lyd'
New-Item -ItemType Directory -Path $output | Out-Null
$report=[ordered]@{schemaVersion=1;collector='order03-restart-20261125-3';expectedExecutableSha256=$ExpectedExecutableSha256;scriptIdentity=(Get-SelectionIdentity $PSCommandPath);startedUtc=[DateTime]::UtcNow.ToString('o');success=$false;stage='service_readiness';sideEffects=@{newDedicatedAccountsMaximum=1;newCharactersMaximum=1;entryMaximum=1;expectedEntryBP=15;clearRewardRequested=$false;restartPerformedByUser=$true};steps=@();recoveryFailureSamples=@();readinessFailureSamples=@()}
function Save-Order03RestartReport {
    [IO.File]::WriteAllText((Join-Path $output 'restart-lyd.json'),(ConvertTo-Json -InputObject $report -Depth 20)+"`n",[Text.UTF8Encoding]::new($false))
}
Save-Order03RestartReport
$session=$null;$start=$null;$active=$false
try {
    if($ReadyAttempts -lt 1 -or $ReadyAttempts -gt 90){throw 'Readiness attempts must be 1..90.'}
    if($ExpectedExecutableSha256 -notmatch '^[a-f0-9]{64}$'){throw 'Expected executable SHA-256 is invalid.'}
    $beforeProcess=Wait-Order03ServiceReady
    $uuid=[Guid]::NewGuid().ToString();$characterName='ios'+[Guid]::NewGuid().ToString('N').Substring(0,12)
    $loginBody=@{uuid=$uuid;clver='2.0.4';resource_platform='iOS';unity_version='6000.3.26f1'} | ConvertTo-Json -Compress
    $report.stage='identity_creation';Save-Order03RestartReport
    $login=Invoke-Order03Request '/local/client/login' $loginBody | ConvertFrom-Json
    $session=[string]$login.sess_key;$userNumber=$login.userid
    if(!$session -or !$userNumber){throw 'Login fields missing.'}
    $connected=Read-Order03Envelope (Invoke-Order03Request '/Connect' ($session+'{}'))
    if($connected.is_user -ne 0){throw 'Dedicated identity is not fresh.'}
    $createBody=@{name=$characterName;arthur_type=1} | ConvertTo-Json -Compress
    $created=Read-Order03Envelope (Invoke-Order03Request '/UserCreate' ($session+$createBody))
    if($created.user.userid -ne $userNumber){throw 'Created identity mismatch.'}
    $initialHome=Read-Order03Envelope (Invoke-Order03Request '/HomeShow' $session)
    $partners=Read-Order03Envelope (Invoke-Order03Request '/TeamBattleSoloPartnerShow' ($session+'{"bossid":30010102}'))
    $selected=@()
    foreach($role in @(2,3,4)) {
        $group=$partners.arthurs | Where-Object {$_.arthur_type -eq $role} | Select-Object -First 1
        $candidate=$group.partners | Select-Object -First 1
        if(!$candidate.userid){throw 'Required partner missing.'}
        $selected+=@{userid=$candidate.userid;arthur_type=$role;deck_idx=$candidate.rental_idx}
    }
    $startBody=@{bossid=30010102;deck_arthur_type=1;deck_arthur_type_idx=0;partner_deck_selects=$selected} | ConvertTo-Json -Depth 8 -Compress
    $report.stage='entry';Save-Order03RestartReport
    $start=Read-Order03Envelope (Invoke-Order03Request '/TeamBattleSoloStart' ($session+$startBody));$active=$true
    $openingHash=Get-Order03ObjectHash $start
    $report.steps+=@{phase='entry';accepted=$true;initialBP=$initialHome.user.bp;waveCount=@($start.battles).Count}
    $report.processBefore=$beforeProcess;$report.stage='waiting_for_user_restart';Save-Order03RestartReport
    Write-Host 'Now restart ONLY the iOS test service window for port 27040.'
    Write-Host 'In that service window press Ctrl+C, then double-click Start-Phone-Test.cmd again.'
    Write-Host 'Keep THIS verification window open. Waiting up to 5 minutes...'
    $afterProcess=$null
    for($attempt=0;$attempt -lt 150;$attempt++) {
        Start-Sleep -Seconds 2
        try{$candidate=Get-Order03TestProcess;if($candidate.startUtc -ne $beforeProcess.startUtc){$afterProcess=$candidate;break}}catch{}
    }
    if(!$afterProcess){throw 'Changed test process was not observed within the wait limit.'}
    $report.processAfter=$afterProcess;$report.stage='post_restart_readiness';Save-Order03RestartReport
    $readyProcess=Wait-Order03ServiceReady 'afterRestart'
    if($readyProcess.startUtc -ne $afterProcess.startUtc){throw 'Test process changed again before recovery.'}
    $report.stage='relogin';Save-Order03RestartReport
    $login=Invoke-Order03RecoveryRequest '/local/client/login' $loginBody | ConvertFrom-Json
    if($login.userid -ne $userNumber -or !$login.sess_key){throw 'Identity changed after restart.'}
    $session=[string]$login.sess_key
    $report.stage='restore_start';Save-Order03RestartReport
    $restored=Read-Order03Envelope (Invoke-Order03RecoveryRequest '/TeamBattleSoloStart' ($session+$startBody))
    $sameSeed=$start.seed -eq $restored.seed;$sameTime=$start.start_time -eq $restored.start_time
    $sameOpening=$openingHash -eq (Get-Order03ObjectHash $restored)
    $report.steps+=@{phase='restore';sameIdentity=$true;sameSeed=$sameSeed;sameStartTime=$sameTime;sameOpeningPayload=$sameOpening}
    if(!$sameSeed -or !$sameTime -or !$sameOpening){throw 'Battle opening changed after restart.'}
    $report.stage='abandon';Save-Order03RestartReport
    $endBody=@{bossid=30010102;progress=0;is_clear=0;input_cmd=@($start.battles | ForEach-Object {''});enemy_dead_bit=@($start.battles | ForEach-Object {0})} | ConvertTo-Json -Depth 5 -Compress
    $ended=Read-Order03Envelope (Invoke-Order03Request '/TeamBattleSoloEnd' ($session+$endBody));$active=$false
    $homeAfter=Read-Order03Envelope (Invoke-Order03Request '/HomeShow' $session)
    $report.steps+=@{phase='abandon';accepted=$true;isClear=$ended.is_clear;finalBP=$homeAfter.user.bp;bpDelta=$initialHome.user.bp-$homeAfter.user.bp}
    if($initialHome.user.bp-$homeAfter.user.bp -ne 15){throw 'Entry debit did not match the single-entry policy.'}
    $report.success=$true;$report.stage='complete'
} catch {
    $report.failureClass=$_.Exception.GetType().Name;$report.failureStage=$report.stage
    $report.failureDiagnostic=@{route=$report.lastRequestRoute;httpStatus=$report.lastHttpStatus;networkStatus=$report.lastNetworkStatus;networkFailureClass=$report.lastNetworkFailureClass;businessCode=$report.lastBusinessCode}
    if($active -and $session -and $start) {
        try {
            $abandon=@{bossid=30010102;progress=0;is_clear=0;input_cmd=@($start.battles | ForEach-Object {''});enemy_dead_bit=@($start.battles | ForEach-Object {0})} | ConvertTo-Json -Depth 5 -Compress
            $null=Read-Order03Envelope (Invoke-Order03Request '/TeamBattleSoloEnd' ($session+$abandon))
            $report.steps+=@{phase='failure_abandon';accepted=$true}
        } catch {$report.steps+=@{phase='failure_abandon';accepted=$false}}
    }
} finally {
    $report.finishedUtc=[DateTime]::UtcNow.ToString('o');Save-Order03RestartReport
    $hash=(Get-FileHash -LiteralPath (Join-Path $output 'restart-lyd.json') -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText((Join-Path $output 'SHA256SUMS-lyd.txt'),$hash+'  restart-lyd.json'+"`n",[Text.UTF8Encoding]::new($false))
    [IO.Compression.ZipFile]::CreateFromDirectory($output,$output+'.zip')
    Write-Host ('Saved: '+$output+'.zip')
    Write-Host ('State: '+$report.stage)
    if(!$report.success){Write-Host ('Failed stage: '+$report.failureStage);Write-Host ('Route: '+$report.failureDiagnostic.route+'; HTTP: '+$report.failureDiagnostic.httpStatus+'; Network: '+$report.failureDiagnostic.networkStatus)}
}
