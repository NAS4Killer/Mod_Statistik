param([string]$UiDirectory)
$ErrorActionPreference='Stop'
$r=$PSScriptRoot
$c=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$out=Join-Path $env:TEMP ('ms-'+[Guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path $out | Out-Null
Get-ChildItem -LiteralPath $r -Filter '*.dll' | Where-Object Name -ne 'Mod_Statistik.dll' | Copy-Item -Destination $out
function Test-Case([string]$Name,[string[]]$Sources,[string[]]$RunArgs,[string[]]$Extra=@()) {
 $exe=Join-Path $out ($Name+'.exe')
 $a=@('/nologo','/codepage:65001','/target:exe',('/out:'+$exe),'/reference:System.Core.dll','/reference:System.Runtime.Serialization.dll')+$Extra
 foreach($s in $Sources){$a+=Join-Path $r $s}
 & $c @a
 if($LASTEXITCODE -ne 0){throw "Testbuild fehlgeschlagen: $Name"}
 & $exe @RunArgs
 if($LASTEXITCODE -ne 0){throw "Test fehlgeschlagen: $Name"}
}
$fixture=Join-Path $r 'Data\bloodmoon_kills.txt'
Test-Case 'Log' @('src\BloodmoonLog.cs','src\BloodmoonAccuracy.cs','tests\BloodmoonLogTests.cs') @($fixture)
Test-Case 'Recorder' @('src\BloodmoonLog.cs','src\BloodmoonAccuracy.cs','src\BloodmoonRecorder.cs','tests\BloodmoonRecorderTests.cs') @($out,$fixture)
Test-Case 'Hud' @('src\BloodmoonLog.cs','src\BloodmoonAccuracy.cs','src\BloodmoonHudDisplay.cs','tests\BloodmoonHudDisplayTests.cs') @()
Test-Case 'Sync' @('src\BloodmoonLog.cs','src\BloodmoonAccuracy.cs','src\BloodmoonRecorder.cs','src\BloodmoonRecording.cs','src\BloodmoonHudDisplay.cs','src\StatisticsWire.cs','src\StatisticsMessage.cs','src\StatisticsTransport.cs','src\StatisticsSync.cs','src\BloodmoonAccuracyRuntime.cs','src\StatisticsDelta.cs','tests\StatisticsSyncTests.cs') @($fixture,(Join-Path $out 'sync')) @(('/reference:'+(Join-Path $r '0Harmony.dll')))
Test-Case 'Shots' @('src\BloodmoonShots.cs','tests\BloodmoonShotsTests.cs') @()
Test-Case 'Headshots' @('src\BloodmoonShots.cs','src\BloodmoonLog.cs','src\BloodmoonAccuracy.cs','src\StatisticsWire.cs','src\StatisticsMessage.cs','src\StatisticsDelta.cs','tests\BloodmoonHeadshotsTests.cs') @((Join-Path $out 'headshots'))
Test-Case 'Accuracy' @('src\BloodmoonLog.cs','src\BloodmoonAccuracy.cs','tests\BloodmoonAccuracyTests.cs') @((Join-Path $out 'accuracy'))
Test-Case 'AccuracyWire' @('src\BloodmoonLog.cs','src\BloodmoonAccuracy.cs','src\StatisticsWire.cs','src\StatisticsMessage.cs','src\StatisticsDelta.cs','tests\StatisticsAccuracyWireTests.cs') @()
Test-Case 'Rows' @('src\BloodmoonLog.cs','src\BloodmoonAccuracy.cs','src\StatisticsRow.cs','tests\StatisticsRowTests.cs') @($fixture)
Test-Case 'AccuracySync' @('src\BloodmoonLog.cs','src\BloodmoonAccuracy.cs','src\BloodmoonRecorder.cs','src\BloodmoonRecording.cs','src\BloodmoonAccuracyRuntime.cs','src\BloodmoonHudDisplay.cs','src\StatisticsWire.cs','src\StatisticsDelta.cs','src\StatisticsMessage.cs','src\StatisticsTransport.cs','src\StatisticsSync.cs','tests\StatisticsAccuracySyncTests.cs') @((Join-Path $out 'accuracy-sync')) @(('/reference:'+(Join-Path $r '0Harmony.dll')))
Test-Case 'ShotHooks' @('src\BloodmoonShots.cs','src\BloodmoonShotHooks.cs','tests\BloodmoonShotHooksTests.cs') @() @(('/reference:'+(Join-Path $r '0Harmony.dll')))
Test-Case 'AccuracyRanking' @('src\BloodmoonLog.cs','src\BloodmoonAccuracy.cs','src\BloodmoonAccuracyRanking.cs','tests\BloodmoonAccuracyRankingTests.cs') @()
Test-Case 'HudAccuracyUi' @('src\BloodmoonLog.cs','src\BloodmoonAccuracy.cs','src\BloodmoonAccuracyRanking.cs','src\GroupKillsHud.cs','src\GroupKillsEntry.cs','src\LiveKillsHud.cs','tests\BloodmoonHudAccuracyUiTests.cs') @()
if($UiDirectory){
 $extra=@('/reference:System.Xml.Linq.dll',('/reference:'+(Join-Path $r '0Harmony.dll')))
 foreach($n in @('windows','templates','xui')){$extra+='/resource:'+(Join-Path $r ('src\LocalUI\'+$n+'.xml'))+',ModStatistik.LocalUI.'+$n+'.xml'}
 Test-Case 'LocalUi' @('src\LocalStatisticsUi.cs','tests\LocalStatisticsUiTests.cs') @($UiDirectory) $extra
}
Write-Output "Tests bestanden. Testartefakte: $out"
