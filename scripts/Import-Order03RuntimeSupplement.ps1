param([Parameter(Mandatory=$true)][string]$Archive)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$expectedHash='cf68822b1837e8f61845236c3c170eb535996a1e73cd419a233dc46b0bc3b964'
$archiveFile=Get-Item -LiteralPath $Archive
if($archiveFile.PSIsContainer -or $archiveFile.Length -ne 72483 -or (Get-FileHash -LiteralPath $archiveFile.FullName -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedHash){throw 'Unexpected runtime supplement archive identity.'}
$destination=Join-Path $root 'inputs/server-order03/Order03-Runtime-Supplement-20261124T010145.658885200Z'
$allowed=@('runtime-observation.json','battle-static-projection.json','normal_quest-static-projection.json')
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=[IO.Compression.ZipFile]::OpenRead($archiveFile.FullName)
$records=@()
try {
    if($zip.Entries.Count -ne 3 -or @($zip.Entries | Select-Object -ExpandProperty FullName -Unique).Count -ne 3){throw 'Unexpected or duplicate archive entries.'}
    foreach($entry in $zip.Entries){
        if($allowed -notcontains $entry.FullName -or $entry.Length -le 0 -or $entry.Length -gt 16777216){throw 'Unexpected archive entry.'}
    }
    if(!(Test-Path -LiteralPath $destination)){New-Item -ItemType Directory -Path $destination | Out-Null}
    $directory=Get-Item -LiteralPath $destination -Force
    if(!$directory.PSIsContainer -or ($directory.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Expected an ordinary project input directory.'}
    foreach($entry in $zip.Entries){
        $stream=$entry.Open()
        $digest=[Security.Cryptography.SHA256]::Create()
        try{$entryHash=([BitConverter]::ToString($digest.ComputeHash($stream))).Replace('-','').ToLowerInvariant()}finally{$stream.Dispose();$digest.Dispose()}
        $target=Join-Path $destination $entry.FullName
        if(!(Test-Path -LiteralPath $target)){[IO.Compression.ZipFileExtensions]::ExtractToFile($entry,$target)}
        $item=Get-Item -LiteralPath $target -Force
        if($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -or $item.Length -ne $entry.Length -or (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entryHash){throw 'Preserved extraction differs from the ZIP entry.'}
        $records += [ordered]@{name=$entry.FullName;bytes=$entry.Length;sha256=$entryHash;matchesExtractedBytes=$true}
    }
}finally{$zip.Dispose()}
if((Get-FileHash -LiteralPath $archiveFile.FullName -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedHash){throw 'Archive changed during import.'}
$report=[ordered]@{
    checkedAt=[DateTime]::UtcNow.ToString('o');archive='Order03-Runtime-Supplement-20261124T010145.658885200Z.zip'
    archiveBytes=72483;archiveSha256=$expectedHash;isolatedCopy='inputs/server-order03/Order03-Runtime-Supplement-20261124T010145.658885200Z'
    entries=$records;allEntryBytesMatch=$true
    scope='User-provided archive and isolated extraction byte integrity only. The package has no separate signed/checksum manifest or collector source. Locally computed hashes identify received bytes; they do not independently authenticate server observation or derivation of static projections from claimed raw-source hashes.'
}
$encoded=($report | ConvertTo-Json -Depth 6)+"`n"
[IO.File]::WriteAllText((Join-Path $root 'evidence/ios-package-search/order03-runtime-supplement-import.json'),$encoded,[Text.UTF8Encoding]::new($false))
Write-Output 'Verified archive identity and all three isolated JSON entry hashes; no server or account access.'
