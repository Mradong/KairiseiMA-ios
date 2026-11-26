param([Parameter(Mandatory=$true)][string]$ArchivePath)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$source=[IO.Path]::GetFullPath($ArchivePath);$name=[IO.Path]::GetFileName($source)
if($name -notmatch '^Order05-RoleImages-[0-9TZ]+-lyd\.zip$'){throw 'Unexpected role-image archive name.'}
$hash=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()
$root=Join-Path $projectRoot 'inputs\server-order05';New-Item -ItemType Directory -Path $root -Force | Out-Null
$copy=Join-Path $root $name;[IO.File]::Copy($source,$copy,$false)
$out=Join-Path $root ([IO.Path]::GetFileNameWithoutExtension($name));if(Test-Path -LiteralPath $out){throw 'Existing import must be preserved.'}
$pins=@{
 'role-1-lyd.dat'=@(157081,'65ab5c22ac5784a634f8f18822a91a11d10284c344f346762f5593b85f580b73')
 'role-2-lyd.dat'=@(118390,'6e6c07dfd589259e9c238c672f80c83a23734656476d6e99a0ecec358e391d08')
 'role-3-lyd.dat'=@(133695,'20068dff42f47418f364b64f0e8b63af99cb8ad029d491942ca155a9d230171c')
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=[IO.Compression.ZipFile]::OpenRead($copy)
try{
 if($zip.Entries.Count -gt 5){throw 'Unexpected extra entries.'};$seen=@{};[long]$total=0
 foreach($entry in $zip.Entries){if($entry.FullName -ne $entry.Name -or $entry.Name -notin (@($pins.Keys)+@('role-images-lyd.json','SHA256SUMS-lyd.txt')) -or $seen.ContainsKey($entry.Name)){throw 'Unsafe/unexpected archive entry.'};$seen[$entry.Name]=$true;$total+=$entry.Length;if($total -gt 500000 -or $entry.Length -gt 200000){throw 'Archive exceeds fixed resource limits.'}}
 New-Item -ItemType Directory -Path $out | Out-Null
 foreach($entry in $zip.Entries){[IO.Compression.ZipFileExtensions]::ExtractToFile($entry,(Join-Path $out $entry.Name),$false)}
}finally{$zip.Dispose()}
$checks=@{}
foreach($line in [IO.File]::ReadAllLines((Join-Path $out 'SHA256SUMS-lyd.txt'))){if($line -notmatch '^([a-f0-9]{64})  ([a-zA-Z0-9_.-]+)$'){throw 'Checksum format invalid.'};if($checks.ContainsKey($Matches[2])){throw 'Duplicate checksum.'};$checks[$Matches[2]]=$Matches[1]}
$records=@()
foreach($file in Get-ChildItem -LiteralPath $out -File){if($file.Name -eq 'SHA256SUMS-lyd.txt'){continue};$actual=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant();if(!$checks.ContainsKey($file.Name)-or$checks[$file.Name]-ne$actual){throw 'Checksum mismatch.'};if($pins.ContainsKey($file.Name)){$pin=$pins[$file.Name];if($file.Length-ne$pin[0]-or$actual-ne$pin[1]){throw 'Original role-image cohort mismatch.'};$records += [pscustomobject]@{file=$file.Name;bytes=$file.Length;sha256=$actual;publishedIdentityMatches=$true}}}
if($checks.Count-ne$records.Count+1){throw 'Checksum inventory mismatch.'}
$receipt=Get-Content -LiteralPath (Join-Path $out 'role-images-lyd.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if($receipt.schemaVersion-ne1-or$receipt.planItem-ne'05'){throw 'Unexpected resource receipt.'}
foreach($row in $receipt.files){if($row.state-eq'COPIED_VERIFIED'){$matched=@($records | Where-Object file -eq $row.exportedFile);if($matched.Count-ne1-or$matched[0].sha256-ne$row.expectedSha256){throw 'Receipt identity differs.'}}}
if((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()-ne$hash){throw 'Original ZIP changed.'}
$report=[ordered]@{checkedAt=[DateTime]::UtcNow.ToString('o');planItem='05';archive=$name;archiveSha256=$hash;archiveBytes=(Get-Item -LiteralPath $copy).Length;extraction=$out.Substring($projectRoot.Length+1).Replace('\','/');checksumEntriesVerified=$checks.Count;resources=$records;allThreeRolesVerified=($records.Count-eq3);accountDataRead=$false;gameRequests=0;renderingVerified=$false;iOSCompiled=$false}
$report | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $projectRoot 'evidence\ios-package-search\order05-role-image-import-validation-lyd.json') -Encoding UTF8
$report | ConvertTo-Json -Depth 7
