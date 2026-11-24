param(
    [string]$TestRoot = $PSScriptRoot,
    [string]$SourceRoot,
    [string]$ResourceManifest,
    [string]$ExpectedTables,
    [int]$Port = 27040
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Windows PowerShell 5.1. All writes belong to a new collection directory.
$collectionStarted = [DateTime]::UtcNow
$testDirectory = (Get-Item -LiteralPath $TestRoot -Force).FullName
if (!(Test-Path -LiteralPath $testDirectory -PathType Container)) { throw 'TestRoot must be a directory.' }
$runName = 'Order03-Remaining-' + $collectionStarted.ToString('yyyyMMddTHHmmssfffZ') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 6)
$outputDirectory = Join-Path $testDirectory $runName
$zipPath = $outputDirectory + '.zip'
$null = New-Item -ItemType Directory -Path $outputDirectory
$issues = New-Object 'System.Collections.Generic.List[object]'
$fileRecords = New-Object 'System.Collections.Generic.List[object]'
$anchors = New-Object 'System.Collections.Generic.List[string]'

function Write-Json([string]$Relative, $Value) {
    $target = Join-Path $outputDirectory $Relative
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force
    [IO.File]::WriteAllText($target, (ConvertTo-Json -InputObject $Value -Depth 30) + "`n", [Text.UTF8Encoding]::new($false))
}
function Add-Issue([string]$Area, [string]$Reason) {
    $issues.Add([ordered]@{ area=$Area; reason=$Reason })
}
function Get-PlainFile([string]$Filename) {
    $item = Get-Item -LiteralPath $Filename -Force
    if ($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Ordinary file required.' }
    $parent = $item.Directory
    while ($parent) {
        if ($parent.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse point in file path.' }
        $parent = $parent.Parent
    }
    return $item
}
function Get-Identity([string]$Filename) {
    $before = Get-PlainFile $Filename
    $bytes = $before.Length
    $modified = $before.LastWriteTimeUtc
    $digest = (Get-FileHash -LiteralPath $before.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $after = Get-PlainFile $Filename
    return [ordered]@{ bytes=$bytes; sha256=$digest; lastWriteUtc=$modified.ToString('o'); stable=($bytes -eq $after.Length -and $modified -eq $after.LastWriteTimeUtc) }
}
function Add-Anchor([string]$Directory) {
    if (!$Directory -or !(Test-Path -LiteralPath $Directory -PathType Container)) { return }
    $full = (Get-Item -LiteralPath $Directory).FullName
    if (!$anchors.Contains($full)) { $anchors.Add($full) }
}
function Resolve-Locator([string]$Base, [string]$Value) {
    if (!$Value -or $Value -match '[\r\n\x00]' -or $Value -match '^[a-z]+://') { return $null }
    if ([IO.Path]::IsPathRooted($Value)) { return [IO.Path]::GetFullPath($Value) }
    return [IO.Path]::GetFullPath((Join-Path $Base $Value))
}
function Has-SensitiveLiteral([string]$Text, [bool]$JsonDocument = $false) {
    if ($Text -match '-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----|\b(?:ghp_|github_pat_|sk-)[A-Za-z0-9_-]{20,}|\bAKIA[0-9A-Z]{16}\b') { return $true }
    if ($Text -match '(?i)(?:password|passwd|secret|sess_key|api_key|access_token)\s*[=:]\s*["''][^"'']{12,}["'']') { return $true }
    if ($JsonDocument -and $Text -match '(?i)"(?:uuid|sess_key|user_id|account_id|password|token|access_token|private_key)"\s*:') { return $true }
    return $false
}
function Copy-Evidence([string]$Filename, [string]$Relative, [bool]$JsonDocument = $false) {
    $file = Get-PlainFile $Filename
    $identity = Get-Identity $Filename
    if ($file.Length -gt 33554432 -or !$identity.stable) {
        Add-Issue $Relative 'Oversized or changing input was omitted.'
        return $false
    }
    $text = [IO.File]::ReadAllText($Filename, [Text.Encoding]::UTF8)
    if (Has-SensitiveLiteral $text $JsonDocument) {
        $fileRecords.Add([ordered]@{ role=$Relative; state='omitted_sensitive_content'; identity=$identity })
        return $false
    }
    $destination = Join-Path $outputDirectory $Relative
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force
    [IO.File]::Copy($Filename, $destination, $false)
    $captured = Get-Identity $destination
    $after = Get-Identity $Filename
    $same = $captured.sha256 -eq $identity.sha256 -and $after.sha256 -eq $identity.sha256 -and $after.stable
    if (!$same) {
        Remove-Item -LiteralPath $destination
        Add-Issue $Relative 'Input changed during capture; captured file removed.'
        return $false
    }
    $fileRecords.Add([ordered]@{ role=$Relative; state='copied_original_bytes'; identity=$identity })
    return $true
}

function Read-GoString([IO.Stream]$Stream) {
    [long]$length = 0
    $shift = 0
    do {
        $value = $Stream.ReadByte()
        if ($value -lt 0 -or $shift -gt 56) { throw 'Invalid Go string length.' }
        $length = $length -bor ([long]($value -band 127) -shl $shift)
        $shift += 7
    } while ($value -band 128)
    if ($length -gt 1048576) { throw 'Go build metadata is too large.' }
    $data = New-Object byte[] ([int]$length)
    $offset = 0
    while ($offset -lt $data.Length) {
        $read = $Stream.Read($data, $offset, $data.Length - $offset)
        if ($read -eq 0) { throw 'Truncated Go build metadata.' }
        $offset += $read
    }
    return [Text.Encoding]::UTF8.GetString($data)
}
function Read-GoBuildInfo([string]$Filename) {
    $stream = [IO.File]::Open($Filename, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
    try {
        $encoding = [Text.Encoding]::GetEncoding(28591)
        $signature = [char]255 + ' Go buildinf:'
        $buffer = New-Object byte[] 1048576
        [long]$position = 0
        while ($position -lt $stream.Length) {
            $stream.Position = $position
            $count = $stream.Read($buffer, 0, $buffer.Length)
            $index = $encoding.GetString($buffer, 0, $count).IndexOf($signature, [StringComparison]::Ordinal)
            if ($index -ge 0) {
                $stream.Position = $position + $index
                $header = New-Object byte[] 32
                if ($stream.Read($header, 0, 32) -ne 32) { throw 'Truncated Go header.' }
                if (($header[15] -band 2) -eq 0) { return [ordered]@{ state='unsupported_pointer_format' } }
                $version = Read-GoString $stream
                $module = Read-GoString $stream
                if ($module.Length -ge 33 -and $module[$module.Length - 17] -eq "`n") { $module = $module.Substring(16, $module.Length - 32) }
                $records = @()
                foreach ($line in $module.Split("`n")) {
                    $fields = $line.Split("`t")
                    if ($fields[0] -in @('path','mod','dep') -and $fields.Length -ge 2) {
                        $records += [ordered]@{ kind=$fields[0]; path=$fields[1]; version=$(if ($fields.Length -ge 3) { $fields[2] } else { $null }) }
                    }
                }
                $settings = [ordered]@{}
                foreach ($line in $module.Split("`n")) {
                    if ($line -match '^build\t([^=]+)=(.*)$') {
                        $key = $Matches[1]; $value = $Matches[2].Trim('"')
                        if ($key -in @('GOOS','GOARCH','GOAMD64','CGO_ENABLED','-buildmode','-compiler','-trimpath','vcs','vcs.revision','vcs.time','vcs.modified')) { $settings[$key] = $value }
                        if ($key -eq '-ldflags') {
                            $settings['ldflagsStripSymbols'] = [bool]($value -match '(^|\s)-s(\s|$)')
                            $settings['ldflagsStripDwarf'] = [bool]($value -match '(^|\s)-w(\s|$)')
                            $settings['ldflagsHasXOverride'] = [bool]($value -match '(^|\s)-X(\s|=)')
                            $digest = [Security.Cryptography.SHA256]::Create()
                            try { $settings['ldflagsSha256'] = ([BitConverter]::ToString($digest.ComputeHash([Text.Encoding]::UTF8.GetBytes($value)))).Replace('-','').ToLowerInvariant() } finally { $digest.Dispose() }
                        }
                    }
                }
                return [ordered]@{ state='decoded'; goVersion=$version; modules=$records; settings=$settings; scope='Metadata embedded in the disk executable. Missing VCS metadata remains unknown.' }
            }
            if ($count -le 31) { break }
            $position += $count - 31
        }
        return [ordered]@{ state='not_found' }
    } finally { $stream.Dispose() }
}

function Get-ListenerObservation {
    try {
        $ids = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction Stop | Select-Object -ExpandProperty OwningProcess -Unique)
        $observations = @()
        foreach ($processNumber in $ids) {
            $process = Get-Process -Id $processNumber -ErrorAction Stop
            $image = $process.MainModule.FileName
            $observations += [ordered]@{ startUtc=$process.StartTime.ToUniversalTime().ToString('o'); imageIdentity=(Get-Identity $image) }
            Add-Anchor (Split-Path -Parent $image)
            Add-Anchor (Split-Path -Parent (Split-Path -Parent $image))
            $script:observedBinary = $image
        }
        return [ordered]@{ state='observed'; port=$Port; listeners=$observations }
    } catch { return [ordered]@{ state='not_listening_or_access_denied'; port=$Port; listeners=@() } }
}
function Invoke-SourceGit([string]$Executable, [string]$Directory, [string[]]$Arguments) {
    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $result = & $Executable -c ('safe.directory=' + $Directory.Replace('\','/')) -C $Directory @Arguments 2>$null
        if ($LASTEXITCODE -eq 0) { return [ordered]@{ success=$true; lines=@($result) } }
        return [ordered]@{ success=$false; lines=@() }
    } finally { $ErrorActionPreference = $previousPreference }
}
function Get-StartupLogProjection([string]$Filename) {
    $stream = [IO.File]::Open($Filename, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
    try {
        $tailStart = [Math]::Max(0, $stream.Length - 1048576)
        $stream.Position = $tailStart
        $buffer = New-Object byte[] ([int]($stream.Length - $tailStart))
        $count = $stream.Read($buffer, 0, $buffer.Length)
        $lines = [Text.Encoding]::UTF8.GetString($buffer, 0, $count).Split("`n")
        $records = @()
        foreach ($line in $lines) {
            if ($line -notmatch '^\s*\{' -or $line.Length -gt 65536) { continue }
            try { $record = $line | ConvertFrom-Json } catch { continue }
            if ($record.msg -notmatch '^(validated CN runtime content gate|loaded (?:CN |combat |runtime |resource )|server listening|HTTP server listening|server started)') { continue }
            $event = $Matches[0].Trim()
            $fields = [ordered]@{}
            foreach ($property in $record.PSObject.Properties) {
                if ($property.Name -match '(?i)^(port|battle_port|team_battle_groups|team_battle_boss_entries|stage_quest_stages|table_count|card_count|skill_count|enemy_count|config_sha256|manifest_sha256|resource_set_sha256|resource_manifest_sha256|tables_sha256)$' -and ($property.Value -is [ValueType] -or [string]$property.Value -match '^[0-9a-f]{64}$')) { $fields[$property.Name] = $property.Value }
                if ($property.Name -in @('resource_set','resource_manifest','master_path','table_path','config_path') -and $property.Value -is [string]) {
                    $path = Resolve-Locator (Split-Path -Parent $Filename) $property.Value
                    if ($path -and (Test-Path -LiteralPath $path -PathType Leaf)) { $fields[$property.Name + '_disk_identity_at_collection'] = Get-Identity $path }
                }
            }
            $timestamp = $null
            if ([string]$record.time -match '^\d{4}-\d{2}-\d{2}T') { $timestamp = [string]$record.time }
            $records += [ordered]@{ time=$timestamp; event=$event; fields=$fields }
        }
        return ,$records
    } finally { $stream.Dispose() }
}

Write-Host 'Collecting existing deployment, source and static-resource evidence...'
$observedBinary = $null
Add-Anchor $testDirectory
$beforeProcess = Get-ListenerObservation
$configPath = Join-Path $testDirectory 'isolated-config.json'
$configIdentity = $null
$configLocators = @()
if (Test-Path -LiteralPath $configPath -PathType Leaf) {
    try {
        $configIdentity = Get-Identity $configPath
        if ($configIdentity.bytes -gt 1048576) { throw 'Config too large.' }
        $config = [IO.File]::ReadAllText($configPath, [Text.Encoding]::UTF8) | ConvertFrom-Json
        foreach ($property in $config.PSObject.Properties) {
            if ($property.Name -match '(?i)(resource|original|source|server|package)' -and $property.Name -notmatch '(?i)(token|password|secret|save|account|database)' -and $property.Value -is [string]) {
                $resolved = Resolve-Locator $testDirectory $property.Value
                if ($resolved -and (Test-Path -LiteralPath $resolved)) {
                    if (Test-Path -LiteralPath $resolved -PathType Container) { Add-Anchor $resolved }
                    elseif ([IO.Path]::GetFileName($resolved) -eq 'resource-set.json' -and !$ResourceManifest) { $ResourceManifest = $resolved }
                    $configLocators += [ordered]@{ key=$property.Name; basename=[IO.Path]::GetFileName($resolved); exists=$true }
                }
            }
        }
    } catch { Add-Issue 'config' 'Could not read the config identity or its resource locators.' }
} else { Add-Issue 'config' 'isolated-config.json was not found beside the collector.' }
if ($env:USERPROFILE) { Add-Anchor (Join-Path $env:USERPROFILE 'Desktop\ma\Server') }

if (!$observedBinary) {
    foreach ($anchor in $anchors) {
        foreach ($relative in @('bin\kairi-server.exe','kairi-server.exe')) {
            $candidate = Join-Path $anchor $relative
            if (Test-Path -LiteralPath $candidate -PathType Leaf) { $observedBinary = $candidate; break }
        }
        if ($observedBinary) { break }
    }
}
$binaryIdentity = $null; $buildInfo = [ordered]@{ state='binary_not_found' }
if ($observedBinary) {
    try { $binaryIdentity = Get-Identity $observedBinary; $buildInfo = Read-GoBuildInfo $observedBinary }
    catch { $buildInfo = [ordered]@{ state='unavailable_or_invalid' }; Add-Issue 'binary' 'Could not decode the selected binary build metadata.' }
}

if (!$ResourceManifest) {
    foreach ($anchor in $anchors) {
        foreach ($relative in @('resource-set\resource-set.json','resource-set.json')) {
            $candidate = Join-Path $anchor $relative
            if (Test-Path -LiteralPath $candidate -PathType Leaf) { $ResourceManifest = $candidate; break }
        }
        if ($ResourceManifest) { break }
    }
}
$resourceIdentity = $null; $entrypoints = [ordered]@{}; $tableRecords = @(); $staticRecords = @()
if ($ResourceManifest) {
    try {
        $resourceIdentity = Get-Identity $ResourceManifest
        if ($resourceIdentity.bytes -gt 33554432) { throw 'Resource manifest too large.' }
        $manifest = [IO.File]::ReadAllText($ResourceManifest, [Text.Encoding]::UTF8) | ConvertFrom-Json
        $manifestBase = Split-Path -Parent ([IO.Path]::GetFullPath($ResourceManifest))
        foreach ($key in @('cn-combat-card-master','cn-combat-master-root','cn-battle-master')) {
            $value = $manifest.entrypoints.PSObject.Properties[$key].Value
            if ($value -is [string]) { $entrypoints[$key] = Resolve-Locator $manifestBase $value }
        }
        if (!$ExpectedTables) {
            $ExpectedTables = Join-Path $PSScriptRoot 'order03-expected-tables.json'
            if (!(Test-Path -LiteralPath $ExpectedTables)) { $ExpectedTables = Join-Path (Split-Path -Parent $PSScriptRoot) 'evidence\ios-package-search\order03-expected-tables.json' }
        }
        $expected = [IO.File]::ReadAllText($ExpectedTables, [Text.Encoding]::UTF8) | ConvertFrom-Json
        foreach ($table in $expected.tables) {
            if ($table.name -notmatch '^[a-z_]+\.csv$') { throw 'Invalid expected table filename.' }
            $rootPath = $entrypoints['cn-combat-master-root']
            if ($table.name -in @('card.csv','buddy.csv','card_alchemist.csv')) { $rootPath = $entrypoints['cn-combat-card-master'] }
            if ($rootPath -and (Test-Path -LiteralPath $rootPath -PathType Leaf)) { $rootPath = Split-Path -Parent $rootPath }
            if ($rootPath -and (Test-Path -LiteralPath (Join-Path $rootPath $table.name) -PathType Leaf)) {
                $identity = Get-Identity (Join-Path $rootPath $table.name)
                $tableRecords += [ordered]@{ name=$table.name; identity=$identity; matchesReceived=($identity.sha256 -eq $table.sha256 -and $identity.bytes -eq $table.bytes) }
            } else { $tableRecords += [ordered]@{ name=$table.name; state='not_found' } }
        }
        $staticCandidates = @()
        if ($entrypoints['cn-battle-master']) {
            $battlePath = $entrypoints['cn-battle-master']
            $staticCandidates += @{ role='battle'; path=$battlePath }
            $normalRoot = if (Test-Path -LiteralPath $battlePath -PathType Container) { $battlePath } else { Split-Path -Parent $battlePath }
            $normalPath = Join-Path $normalRoot 'cn602-normal-quest-runtime.json'
            if (!(Test-Path -LiteralPath $normalPath -PathType Leaf)) { $normalPath = Join-Path $normalRoot 'normal_quest.json' }
            $staticCandidates += @{ role='normal_quest'; path=$normalPath }
        }
        foreach ($selection in $staticCandidates) {
            $staticPath = $selection.path
            if (Test-Path -LiteralPath $staticPath -PathType Container) { $staticPath = Join-Path $staticPath ($selection.role + '.json') }
            if (!(Test-Path -LiteralPath $staticPath -PathType Leaf)) { $staticRecords += @{ role=$selection.role; state='not_found' }; continue }
            $identity = Get-Identity $staticPath
            $copied = Copy-Evidence $staticPath ('static/' + $selection.role + '.json') $true
            $staticRecords += [ordered]@{ role=$selection.role; identity=$identity; rawBytesIncluded=$copied }
        }
    } catch { Add-Issue 'resource_manifest' 'Resource entrypoints or expected-table checks could not be fully resolved.' }
} else { Add-Issue 'resource_manifest' 'No resource-set.json was found in the selected deployment locations.' }

if (!$SourceRoot) {
    foreach ($anchor in $anchors) {
        foreach ($relative in @('server-ios\server','source\server-ios\server','source\server','source-review\kairisei-ma-ch\server','server','src\server','.')) {
            $candidate = Join-Path $anchor $relative
            if ((Test-Path -LiteralPath (Join-Path $candidate 'go.mod')) -and (Test-Path -LiteralPath (Join-Path $candidate 'cmd\kairi-server\main.go'))) { $SourceRoot = $candidate; break }
        }
        if ($SourceRoot) { break }
    }
}
$source = [ordered]@{ state='not_found'; commit='UNKNOWN'; files=@(); scope='Captured source files have no automatic link to the running binary.' }
if ($SourceRoot -and (Test-Path -LiteralPath $SourceRoot -PathType Container)) {
    $moduleRoot = (Get-Item -LiteralPath $SourceRoot).FullName
    $modulePrefix = $moduleRoot.TrimEnd('\') + '\'
    $sourceFiles = New-Object 'System.Collections.Generic.List[object]'
    $pending = New-Object 'System.Collections.Generic.Queue[string]'
    $pending.Enqueue($moduleRoot)
    while ($pending.Count -gt 0) {
        $directory = $pending.Dequeue()
        foreach ($item in Get-ChildItem -LiteralPath $directory -Force) {
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { continue }
            if ($item.PSIsContainer) {
                if ($item.Name -notin @('.git','_local','data','accounts','saves','logs','vendor','node_modules','tools','build','dist','test-output')) { $pending.Enqueue($item.FullName) }
            } elseif ($item.Extension -eq '.go' -or $item.Name -in @('go.mod','go.sum')) {
                $relative = $item.FullName.Substring($modulePrefix.Length).Replace('\','/')
                $copied = Copy-Evidence $item.FullName ('source/' + $relative)
                $sourceFiles.Add([ordered]@{ path=$relative; identity=(Get-Identity $item.FullName); included=$copied })
            }
        }
    }
    $source.state = 'captured'; $source.files = @($sourceFiles.ToArray())
    $git = Get-Command git.exe -ErrorAction SilentlyContinue
    if ($git) {
        $revision = Invoke-SourceGit $git.Source $moduleRoot @('rev-parse','HEAD')
        if ($revision.success -and $revision.lines.Count -eq 1 -and $revision.lines[0] -match '^[0-9a-f]{40}$') { $source.commit = [string]$revision.lines[0] }
        # Record only source paths, never remote URLs or Git config.
        $changes = Invoke-SourceGit $git.Source $moduleRoot @('status','--porcelain','--','*.go','go.mod','go.sum')
        if ($changes.success) { $source.sourceWorkingTreeChanges = $changes.lines; $source.capturedSourceWorkingTreeClean = $changes.lines.Count -eq 0 }
    }
} else { Add-Issue 'source' 'Source was absent from the deployment. Use optional -SourceRoot if it exists elsewhere.' }

$startupNames = @('Start-IOS-Test.ps1','Build-IOS-Test.ps1','Prepare-IOS-Test.ps1','Build-Server.ps1')
foreach ($name in $startupNames) {
    $filename = Join-Path $testDirectory $name
    if (Test-Path -LiteralPath $filename -PathType Leaf) { $null = Copy-Evidence $filename ('deployment-scripts/' + $name) }
}
$provenanceNames = @('build-info.json','build-manifest.json','source-manifest.json','provenance.json','conversion-manifest.json','ios-server.patch','patches\ios-server.patch')
foreach ($name in $provenanceNames) {
    $filename = Join-Path $testDirectory $name
    if (Test-Path -LiteralPath $filename -PathType Leaf) { $null = Copy-Evidence $filename ('provenance/' + $name.Replace('\','/')) ($name.EndsWith('.json')) }
}
$logDirectories = @($testDirectory, (Join-Path $testDirectory 'logs'), (Join-Path $testDirectory '_local\logs'))
$runtimeDirectory = Join-Path $testDirectory '_local\runtime'
if (Test-Path -LiteralPath $runtimeDirectory -PathType Container) {
    $logDirectories += @(Get-ChildItem -LiteralPath $runtimeDirectory -Directory | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 40 -ExpandProperty FullName)
}
$startupObservations = @()
foreach ($directory in $logDirectories) {
    foreach ($name in @('server.log','stdout.log','stderr.log','startup.log','server.jsonl','stdout.jsonl','startup.jsonl','kairi-server.log','kairi-server.jsonl')) {
        $filename = Join-Path $directory $name
        if (!(Test-Path -LiteralPath $filename -PathType Leaf)) { continue }
        try {
            $null = Get-PlainFile $filename
            $records = Get-StartupLogProjection $filename
            $startupObservations += [ordered]@{ basename=$name; records=$records; scope='Selected typed startup events from the final 1 MiB. Paths and unrelated request/account records are omitted.' }
        } catch { Add-Issue 'startup_logs' 'A selected log was unavailable or invalid.' }
    }
}
Write-Json 'startup-observations.json' $startupObservations
# Explicitly supplied, already-redacted evidence can be placed in this directory.
$supplementDirectory = Join-Path $testDirectory 'Order03-Provided-Evidence'
$supplements = @()
if (Test-Path -LiteralPath $supplementDirectory -PathType Container) {
    foreach ($item in Get-ChildItem -LiteralPath $supplementDirectory -File) {
        if ($item.Extension -in @('.json','.md','.txt','.ps1','.patch')) {
            $copied = Copy-Evidence $item.FullName ('provided/' + $item.Name) ($item.Extension -eq '.json')
            $supplements += [ordered]@{ name=$item.Name; included=$copied }
        }
    }
}

$afterProcess = Get-ListenerObservation
$processStable = ($beforeProcess.state -eq 'observed' -and $afterProcess.state -eq 'observed' -and ($beforeProcess | ConvertTo-Json -Depth 8 -Compress) -eq ($afterProcess | ConvertTo-Json -Depth 8 -Compress))
Write-Json 'runtime.json' ([ordered]@{ collectedUtc=[DateTime]::UtcNow.ToString('o'); processBefore=$beforeProcess; processAfter=$afterProcess; sameObservedProcess=$processStable; executable=$binaryIdentity; goBuildInfo=$buildInfo; selectedConfigIdentity=$configIdentity; resolvedConfigLocatorNames=$configLocators; selectedResourceManifestIdentity=$resourceIdentity; tables=$tableRecords; staticResources=$staticRecords; effectiveLoadedBytes='UNKNOWN: resource selection is resolved from disk config and manifest, without inspecting process memory or invocation.' })
Write-Json 'source.json' $source
Write-Json 'provided-evidence.json' $supplements
$requirements = @(
    @{ id='deployed_source'; state='REVIEW_REQUIRED'; available='source.json, runtime.json, source/ and deployment-scripts/'; remaining='Confirm captured source and build flags correspond to the observed executable hash.' },
    @{ id='effective_loaded_resources'; state='UNKNOWN'; remaining='Existing startup/load evidence or deployment build records must tie this process to the selected config and resource hashes.' },
    @{ id='csv_provenance'; state='UNKNOWN'; remaining='Original client region/version, exporter version and CSV transformation sources/records.' },
    @{ id='static_projection_derivation'; state='REVIEW_REQUIRED'; available='static/ original bytes, when found, and runtime.json hashes'; remaining='Reproduce the earlier reduced projections from these original static inputs.' },
    @{ id='successful_continue_and_real_settlement'; state='NOT_TESTED'; remaining='Independent real-engine Continue/End evidence, debit/reward before-after values and identical-request retries.' },
    @{ id='process_restart_recovery'; state='NOT_TESTED'; remaining='Existing isolated-process restart evidence, including durable state and receipt boundaries.' }
)
Write-Json 'remaining.json' $requirements
Write-Json 'capture.json' ([ordered]@{ schemaVersion=1; collector='order03-remaining-20261124-1'; startedUtc=$collectionStarted.ToString('o'); finishedUtc=[DateTime]::UtcNow.ToString('o'); scriptIdentity=(Get-Identity $PSCommandPath); files=@($fileRecords.ToArray()); issues=@($issues.ToArray()); sideEffects=@{ createdEvidenceDirectory=$true; networkRequests=0; gameAccountOperations=0; serviceStartsStopsOrRestarts=0; modifiedInputFiles=0 }; scope='Existing local evidence capture. A missing historical record or unexecuted behavior stays UNKNOWN/NOT_TESTED.' })
$hashes = @()
foreach ($file in Get-ChildItem -LiteralPath $outputDirectory -Recurse -File) {
    $relative = $file.FullName.Substring($outputDirectory.Length + 1).Replace('\','/')
    $hashes += ((Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $relative)
}
[IO.File]::WriteAllText((Join-Path $outputDirectory 'SHA256SUMS.txt'), ($hashes -join "`n") + "`n", [Text.UTF8Encoding]::new($false))
[IO.Compression.ZipFile]::CreateFromDirectory($outputDirectory, $zipPath, [IO.Compression.CompressionLevel]::Optimal, $false)
Write-Host ('Saved: ' + $zipPath)
Write-Host ('SHA256: ' + (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant())
Write-Host 'Return this new ZIP to the client project. Missing evidence is listed in remaining.json.'
