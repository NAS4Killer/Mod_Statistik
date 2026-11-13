param([string]$Commit='HEAD',[string]$OutputDirectory)
$ErrorActionPreference='Stop'
$r=$PSScriptRoot
function Git-Text([string[]]$Arguments){
 $v=& git -c "safe.directory=$($r.Replace('\','/'))" -C $r @Arguments
 if($LASTEXITCODE -ne 0){throw 'Git-Aufruf fehlgeschlagen'}
 return ($v -join "`n")
}
$sha=Git-Text @('rev-parse',($Commit+'^{commit}'))
$version=(Git-Text @('show',($sha+':VERSION'))).Trim()
if($version -notmatch '^\d+\.\d+\.\d+\.\d+$'){throw 'Ungueltige Version'}
if(!$OutputDirectory){$OutputDirectory=Join-Path $r ('releases\'+$version)}
[IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null
$tmp=Join-Path $env:TEMP ('mp-'+[Guid]::NewGuid().ToString('N').Substring(0,8))
[IO.Directory]::CreateDirectory($tmp) | Out-Null
$tar=Join-Path $tmp 'commit.zip'
& git -c "safe.directory=$($r.Replace('\','/'))" -C $r archive --format=zip "--output=$tar" $sha
if($LASTEXITCODE -ne 0){throw 'Commit-Export fehlgeschlagen'}
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory($tar,(Join-Path $tmp 'source'))
$source=Join-Path $tmp 'source'
$files=@('ModInfo.xml','Mod_Statistik.dll','ThirdPartyNotices.txt','SYNC_MODE','Config\Localization.txt','Config\UpdateRepository.txt','Data\bloodmoon_kills.txt','VERSION','README.md',('ReleaseNotes-'+$version+'.txt'),'0Harmony.dll','Mono.Cecil.dll','Mono.Cecil.Mdb.dll','Mono.Cecil.Pdb.dll','Mono.Cecil.Rocks.dll','MonoMod.Backports.dll','MonoMod.Core.dll','MonoMod.Iced.dll','MonoMod.ILHelpers.dll','MonoMod.RuntimeDetour.dll','MonoMod.Utils.dll','System.ValueTuple.dll')
$hashes=@()
foreach($kind in @('client','server')){
 $folder=Join-Path $tmp $kind
 $mod=Join-Path $folder 'Mod_Statistik'
 foreach($name in $files){
  $from=Join-Path $source $name
  if($kind -eq 'server' -and $name -eq 'Mod_Statistik.dll'){$from=Join-Path $source 'server\Mod_Statistik.dll'}
  $to=Join-Path $mod $name
  [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($to)) | Out-Null
  Copy-Item -LiteralPath $from -Destination $to
 }
 $dll=[Reflection.AssemblyName]::GetAssemblyName((Join-Path $mod 'Mod_Statistik.dll'))
 if($dll.Name -ne 'Mod_Statistik' -or $dll.Version.ToString() -ne $version){throw 'DLL passt nicht zur Commit-Version'}
 $suffix=if($kind -eq 'server'){'-server'}else{''}
 $zip=Join-Path $OutputDirectory ('Mod_Statistik-direkt-sync-v'+$version+$suffix+'.zip')
 if(Test-Path -LiteralPath $zip){throw "Ausgabedatei existiert bereits: $zip. Anderen Ausgabeordner waehlen."}
 [IO.Compression.ZipFile]::CreateFromDirectory($folder,$zip)
 $hashes+=((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLower()+'  '+[IO.Path]::GetFileName($zip))
}
$hashes | Set-Content -LiteralPath (Join-Path $OutputDirectory 'SHA256.txt') -Encoding ASCII
Write-Output "Pakete aus Commit $sha erstellt: $OutputDirectory"
