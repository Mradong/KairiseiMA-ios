param([string]$TestRoot = $PSScriptRoot, [int]$Port = 27040)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
if (!('Order03SelectionNative' -as [type])) {
    Add-Type -ReferencedAssemblies System.Runtime.Serialization.dll,System.Xml.dll -TypeDefinition @'
using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

[DataContract]
public sealed class Order03ResourceLocators {
    [DataMember(Name="entrypoints")] public Dictionary<string,string> Entrypoints;
}
[DataContract]
public sealed class Order03PlatformLocators {
    [DataMember(Name="platform_contents")] public string[] Profiles;
}
public static class Order03SelectionNative {
    [DllImport("shell32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
    private static extern IntPtr CommandLineToArgvW(string command, out int count);
    [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr pointer);
    public static string[] Arguments(string command) {
        int count;
        IntPtr pointer = CommandLineToArgvW(command, out count);
        if (pointer == IntPtr.Zero) throw new InvalidOperationException("Cannot decode Windows arguments.");
        try {
            if (count < 1 || count > 256) throw new InvalidOperationException("Unexpected argument count.");
            string[] result = new string[count];
            for (int i=0; i<count; i++) result[i] = Marshal.PtrToStringUni(Marshal.ReadIntPtr(pointer, i*IntPtr.Size));
            return result;
        } finally { LocalFree(pointer); }
    }
    public static T Locators<T>(string filename) {
        var settings = new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat=true, IgnoreExtensionDataObject=true };
        var serializer = new DataContractJsonSerializer(typeof(T), settings);
        using (var stream = File.OpenRead(filename)) return (T)serializer.ReadObject(stream);
    }
    public static Dictionary<string,string> ResourcePaths(string filename) {
        return Locators<Order03ResourceLocators>(filename).Entrypoints;
    }
    public static string[] PlatformPaths(string filename) {
        return Locators<Order03PlatformLocators>(filename).Profiles;
    }
}
'@
}

function Get-SelectionIdentity([string]$Filename) {
    $file = Get-Item -LiteralPath $Filename -Force
    if ($file.PSIsContainer -or ($file.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Ordinary file required.' }
    $directory = $file.Directory
    while ($directory) {
        if ($directory.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse-point input path.' }
        $directory = $directory.Parent
    }
    $length = $file.Length; $modified = $file.LastWriteTimeUtc
    $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $after = Get-Item -LiteralPath $file.FullName
    return [ordered]@{ bytes=$length; sha256=$hash; lastWriteUtc=$modified.ToString('o'); stable=($after.Length -eq $length -and $after.LastWriteTimeUtc -eq $modified) }
}
function Get-ResourceArgumentSelections([string[]]$Arguments) {
    $selected = [ordered]@{ resourceSet=@(); platformProfiles=@(); platformConfig=@() }
    $names = @{ 'resource-set'='resourceSet'; 'platform-content'='platformProfiles'; 'platform-content-config'='platformConfig' }
    for ($index=1; $index -lt $Arguments.Length; $index++) {
        $argument = $Arguments[$index]
        if (!$argument.StartsWith('-')) { continue }
        $token = $argument.TrimStart('-')
        $split = $token.IndexOf('=')
        $name = if ($split -ge 0) { $token.Substring(0,$split) } else { $token }
        if (!$names.ContainsKey($name)) { continue }
        if ($split -ge 0) { $value = $token.Substring($split+1) }
        else {
            $index++
            if ($index -ge $Arguments.Length) { throw 'Selected resource flag has no value.' }
            $value = $Arguments[$index]
        }
        $key = $names[$name]
        if ($name -eq 'platform-content') { $selected[$key] += $value }
        else { $selected[$key] = @($value) }
    }
    return $selected
}
function Resolve-SelectedResource([string]$Base, [string]$Relative) {
    if (!$Relative -or [IO.Path]::IsPathRooted($Relative) -or $Relative -match '[\\:\x00\r\n]' -or @($Relative.Split('/') | Where-Object { $_ -in @('','.','..') }).Count -gt 0) { throw 'Invalid selected resource locator.' }
    $fullBase = [IO.Path]::GetFullPath($Base).TrimEnd('\')
    $filename = [IO.Path]::GetFullPath((Join-Path $fullBase $Relative))
    if (!$filename.StartsWith($fullBase+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Resource escapes its selected root.' }
    return $filename
}
function Read-SelectedResourceFiles([string]$Manifest) {
    $identity = Get-SelectionIdentity $Manifest
    if ($identity.bytes -gt 33554432) { throw 'Resource manifest exceeds the production limit.' }
    $locators = [Order03SelectionNative]::ResourcePaths($Manifest)
    if (!$locators) { throw 'Resource manifest has no entrypoints.' }
    $base = Split-Path -Parent $Manifest
    $cardFile = Resolve-SelectedResource $base $locators['cn-combat-card-master']
    $battleRoot = Resolve-SelectedResource $base $locators['cn-combat-master-root']
    $staticFile = Resolve-SelectedResource $base $locators['cn-battle-master']
    $expectedPath = Join-Path $PSScriptRoot 'order03-expected-tables.json'
    if (!(Test-Path -LiteralPath $expectedPath)) { $expectedPath = Join-Path (Split-Path -Parent $PSScriptRoot) 'evidence\ios-package-search\order03-expected-tables.json' }
    $expected = [IO.File]::ReadAllText($expectedPath,[Text.Encoding]::UTF8) | ConvertFrom-Json
    $tables = @()
    foreach ($entry in $expected.tables) {
        if ($entry.name -notmatch '^[a-z_]+\.csv$') { throw 'Unexpected expected-table filename.' }
        $directory = if ($entry.name -in @('card.csv','buddy.csv','card_alchemist.csv')) { Split-Path -Parent $cardFile } else { $battleRoot }
        $tableIdentity = Get-SelectionIdentity (Join-Path $directory $entry.name)
        $tables += [ordered]@{ name=$entry.name; identity=$tableIdentity; matchesReceived=($tableIdentity.bytes -eq $entry.bytes -and $tableIdentity.sha256 -eq $entry.sha256) }
    }
    $static = @(
        [ordered]@{ role='battle'; identity=(Get-SelectionIdentity $staticFile) },
        [ordered]@{ role='normal_quest'; identity=(Get-SelectionIdentity (Join-Path (Split-Path -Parent $staticFile) 'cn602-normal-quest-runtime.json')) }
    )
    return [ordered]@{ manifest=$identity; tables=$tables; staticResources=$static; manifestStableAfterRead=((Get-SelectionIdentity $Manifest).sha256 -eq $identity.sha256) }
}

# Dot-sourcing exposes only parser/read helpers for local fixture verification.
if ($MyInvocation.InvocationName -eq '.') { return }
$directory = (Get-Item -LiteralPath $TestRoot).FullName
$name = 'Order03-Selection-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,6)
$name += '-lyd'
$output = Join-Path $directory $name
New-Item -ItemType Directory -Path $output | Out-Null
$report = [ordered]@{
    schemaVersion=1; collector='order03-selection-20261124-1'; collectedUtc=[DateTime]::UtcNow.ToString('o'); state='incomplete'
    sourceBoundary='Only the reproduced server executable 7704963e is eligible for process argument inspection.'
    expectedExecutableSha256='7704963e90c587aa4e8167fc7e74299f231e8b22f45a10012fbbe44301727918'
    sideEffects=@{ networkRequests=0; accountOperations=0; serviceChanges=0; inputWrites=0 }
    remaining='Actual memory-loaded bytes, historical CSV extraction and real battle contracts require their own evidence.'
}
$stage = 'listener'
try {
    $owners = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction Stop | Select-Object -ExpandProperty OwningProcess -Unique)
    if ($owners.Count -ne 1) { throw 'Expected one test-service listener.' }
    $processNumber = [int]$owners[0]
    $stage = 'executable_identity'
    $process = Get-Process -Id $processNumber -ErrorAction Stop
    $start = $process.StartTime.ToUniversalTime().ToString('o')
    $binary = $process.MainModule.FileName
    $binaryIdentity = Get-SelectionIdentity $binary
    if (!$binaryIdentity.stable -or $binaryIdentity.sha256 -ne $report.expectedExecutableSha256) { throw 'Listener executable differs from the reproduced source.' }
    $stage = 'process_resource_arguments'
    $record = Get-CimInstance Win32_Process -Filter ('ProcessId = ' + $processNumber) -ErrorAction Stop
    $creationDifference = [Math]::Abs(($record.CreationDate.ToUniversalTime() - $process.StartTime.ToUniversalTime()).TotalMilliseconds)
    if ($creationDifference -gt 1 -or ![string]::Equals([IO.Path]::GetFullPath($record.ExecutablePath),[IO.Path]::GetFullPath($binary),[StringComparison]::OrdinalIgnoreCase)) { throw 'Selected process identity changed.' }
    if (!$record.CommandLine) { throw 'Selected process arguments unavailable.' }
    $arguments = [Order03SelectionNative]::Arguments([string]$record.CommandLine)
    $selected = Get-ResourceArgumentSelections $arguments
    $report.process = [ordered]@{ port=$Port; startUtc=$start; executable=$binaryIdentity }
    $report.selectionCounts = [ordered]@{ resourceSet=$selected.resourceSet.Count; platformProfiles=$selected.platformProfiles.Count; platformConfig=$selected.platformConfig.Count }
    if ($selected.resourceSet.Count -ne 1 -or ![IO.Path]::IsPathRooted($selected.resourceSet[0])) { throw 'An explicit absolute resource-set argument is required to establish selection.' }
    $stage = 'selected_resource_files'
    $report.selectedResources = Read-SelectedResourceFiles $selected.resourceSet[0]
    $profiles = @($selected.platformProfiles)
    $stage = 'selected_platform_profiles'
    if ($selected.platformConfig.Count -gt 0) {
        if ($profiles.Count -gt 0 -or ![IO.Path]::IsPathRooted($selected.platformConfig[0])) { throw 'Ambiguous or relative platform selection.' }
        $configFile = $selected.platformConfig[0]
        $report.platformSelectionConfig = Get-SelectionIdentity $configFile
        foreach ($relative in [Order03SelectionNative]::PlatformPaths($configFile)) { $profiles += Resolve-SelectedResource (Split-Path -Parent $configFile) $relative }
    }
    $report.platformProfiles = @()
    foreach ($profile in $profiles) {
        if (![IO.Path]::IsPathRooted($profile)) { throw 'Relative platform profile selection remains unresolved.' }
        $profileIdentity = Get-SelectionIdentity $profile
        if ($profileIdentity.bytes -gt 1048576) { throw 'Selected platform profile exceeds capture limit.' }
        $profileData = [IO.File]::ReadAllText($profile,[Text.Encoding]::UTF8) | ConvertFrom-Json
        $report.platformProfiles += [ordered]@{ identity=$profileIdentity; platform=[string]$profileData.platform; unityVersion=[string]$profileData.unity_version; contentId=[string]$profileData.content_id }
    }
    $after = Get-Process -Id $processNumber -ErrorAction Stop
    $stage = 'final_process_identity'
    $ownerAfter = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction Stop | Select-Object -ExpandProperty OwningProcess -Unique)
    $report.sameObservedProcess = ($after.StartTime.ToUniversalTime().ToString('o') -eq $start -and $ownerAfter.Count -eq 1 -and $ownerAfter[0] -eq $processNumber)
    if (!$report.sameObservedProcess) { throw 'Test service changed during capture.' }
    $report.executableStableAfterCapture = ((Get-SelectionIdentity $binary).sha256 -eq $binaryIdentity.sha256)
    if (!$report.executableStableAfterCapture) { throw 'Selected executable changed during capture.' }
    $report.state = 'selected_paths_hashed'
} catch {
    $report.failureClass = $_.Exception.GetType().Name
    $report.failureStage = $stage
    $report.failureReason = 'Selection could not be completed. Resource selection state is incomplete.'
}
# Export only selected file identities and typed public profile fields.
[IO.File]::WriteAllText((Join-Path $output 'selection-lyd.json'),(ConvertTo-Json -InputObject $report -Depth 15)+"`n",[Text.UTF8Encoding]::new($false))
$digest = (Get-FileHash -LiteralPath (Join-Path $output 'selection-lyd.json') -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $output 'SHA256SUMS-lyd.txt'),$digest+'  selection-lyd.json'+"`n",[Text.UTF8Encoding]::new($false))
[IO.Compression.ZipFile]::CreateFromDirectory($output,$output+'.zip',[IO.Compression.CompressionLevel]::Optimal,$false)
Write-Host ('Saved: '+$output+'.zip')
Write-Host ('State: '+$report.state)
