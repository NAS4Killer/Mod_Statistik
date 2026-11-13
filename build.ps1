param(
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die',
    [string]$ManagedDir,
    [string]$OutputPath
)
$ErrorActionPreference = 'Stop'
$repo = $PSScriptRoot
$managed = if ($ManagedDir) { $ManagedDir } else { Join-Path $GameDir '7DaysToDie_Data\Managed' }
if (!$OutputPath) { $OutputPath = Join-Path $repo 'Mod_Statistik.dll' }
$OutputPath = [IO.Path]::GetFullPath($OutputPath)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($OutputPath)) | Out-Null
if ([IO.Path]::GetFileName($OutputPath) -ne 'Mod_Statistik.dll') { throw 'DLL-Dateiname muss Mod_Statistik.dll sein (Assemblyname).' }
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$references = @('mscorlib.dll','netstandard.dll','System.dll','System.Core.dll','System.Xml.dll','System.Xml.Linq.dll','System.Net.Http.dll','System.Runtime.Serialization.dll','System.IO.Compression.dll',
    'UnityEngine.CoreModule.dll','UnityEngine.InputLegacyModule.dll','Assembly-CSharp.dll','NGUI.dll','Assembly-CSharp-firstpass.dll')
$arguments = @('/nologo','/codepage:65001','/noconfig','/nostdlib+','/target:library','/optimize+',('/out:' + $OutputPath))
foreach ($name in $references) {
    $reference = Join-Path $managed $name
    if (!(Test-Path -LiteralPath $reference)) { throw "Spielreferenz fehlt: $reference" }
    $arguments += '/reference:' + $reference
}
$arguments += '/reference:' + (Join-Path $repo '0Harmony.dll')
$arguments += '/resource:' + (Join-Path $repo 'src\UpdateInstaller.ps1') + ',ModStatistik.UpdateInstaller.ps1'
foreach ($uiName in @('windows','templates','xui')) { $arguments += '/resource:' + (Join-Path $repo ('src\LocalUI\' + $uiName + '.xml')) + ',ModStatistik.LocalUI.' + $uiName + '.xml' }
$arguments += (Get-ChildItem -LiteralPath (Join-Path $repo 'src') -Filter '*.cs').FullName
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Kompilierung fehlgeschlagen.' }
$version = (Get-Content -LiteralPath (Join-Path $repo 'VERSION') -Raw).Trim()
$assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($OutputPath).Version.ToString()
if ($assemblyVersion -ne $version) { throw 'DLL-Version passt nicht zur VERSION-Datei.' }
Write-Output "Mod_Statistik $version erfolgreich gebaut."
