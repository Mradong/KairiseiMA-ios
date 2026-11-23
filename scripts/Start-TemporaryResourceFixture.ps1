param(
    [Parameter(Mandatory=$true)][string]$Address,
    [Parameter(Mandatory=$true)][string]$RemoteAddress,
    [Parameter(Mandatory=$true)][string]$ControlDirectory
)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$control=[IO.Path]::GetFullPath($ControlDirectory)
if (!$control.StartsWith($root+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or !(Test-Path -LiteralPath $control -PathType Container)) { throw 'Control directory must already exist in the recovery workspace.' }
$ip=$null; $remote=$null
if (![Net.IPAddress]::TryParse($Address,[ref]$ip) -or $ip.AddressFamily -ne [Net.Sockets.AddressFamily]::InterNetwork -or ![Net.IPAddress]::TryParse($RemoteAddress,[ref]$remote) -or $remote.AddressFamily -ne [Net.Sockets.AddressFamily]::InterNetwork) { throw 'Explicit IPv4 addresses are required.' }
if (!(Get-NetIPAddress -AddressFamily IPv4 -InterfaceAlias WLAN -ErrorAction Stop | Where-Object { $_.IPAddress -eq $Address -and $_.AddressState -eq 'Preferred' })) { throw 'Fixture address is not currently assigned to WLAN.' }
if ((Get-NetConnectionProfile -InterfaceAlias WLAN -ErrorAction Stop).NetworkCategory -ne 'Private') { throw 'The hotspot must use the private network profile.' }
$gateway=@(Get-NetRoute -AddressFamily IPv4 -InterfaceAlias WLAN -DestinationPrefix '0.0.0.0/0' -ErrorAction Stop | Where-Object NextHop -eq $RemoteAddress)
if (!$gateway.Count) { throw 'Remote address must match the current WLAN gateway.' }
$name='KairiseiMA-iOS-ResourceFixture-27108'
if (Get-NetFirewallRule -Name $name -ErrorAction SilentlyContinue) { throw 'A firewall rule with this name already exists.' }
if (Get-NetTCPConnection -LocalPort 27108 -State Listen -ErrorAction SilentlyContinue) { throw 'Fixture port is already in use.' }
if (Test-Path -LiteralPath (Join-Path $control 'stop')) { throw 'Control directory must not contain a stop signal.' }
$exe=Join-Path $root 'tools\node-v24.22.3-win-x64\node.exe'
$script=Join-Path $root 'scripts\resource-failure-fixture.mjs'
$created=$false; $server=$null; $failure=$null
try {
    New-NetFirewallRule -Name $name -DisplayName 'KairiseiMA iOS temporary resource fixture' -Direction Inbound -Action Allow -Program $exe -Protocol TCP -LocalPort 27108 -RemoteAddress $RemoteAddress -Profile Private | Out-Null
    $created=$true
    $server=Start-Process -FilePath $exe -ArgumentList @(('"'+$script+'"'),$Address,'27108') -WindowStyle Hidden -RedirectStandardOutput (Join-Path $control 'server.log') -RedirectStandardError (Join-Path $control 'server-error.log') -PassThru
    $deadline=[DateTime]::UtcNow.AddMinutes(20)
    @{ state='running'; address=$Address; remoteAddress=$RemoteAddress; port=27108; pid=$server.Id; expiresUtc=$deadline.ToString('o') } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $control 'status.json') -Encoding UTF8
    while ([DateTime]::UtcNow -lt $deadline -and !(Test-Path -LiteralPath (Join-Path $control 'stop'))) {
        $server.Refresh(); if ($server.HasExited) { throw 'Fixture process exited unexpectedly.' }
        Start-Sleep -Seconds 2
    }
} catch { $failure=$_.Exception.Message }
finally {
    if ($server) { $server.Refresh(); if (!$server.HasExited) { Stop-Process -Id $server.Id -ErrorAction SilentlyContinue } }
    if ($created) { Remove-NetFirewallRule -Name $name -ErrorAction SilentlyContinue }
    $remaining=[bool](Get-NetFirewallRule -Name $name -ErrorAction SilentlyContinue)
    @{ state='stopped'; error=$failure; firewallRuleRemoved=(-not $remaining) } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $control 'status.json') -Encoding UTF8
}
if ($failure) { throw $failure }
