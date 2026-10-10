using System;using System.IO;using System.IO.Compression;using System.Linq;
namespace ModStatistik {internal static class StatisticsRuntime {public const string Version="0.2.0.0";}}
class TestUpdater {
static void Need(bool ok){if(!ok)throw new Exception("Pruefung fehlgeschlagen");}
static int Main(string[] args){try{
Need(StatisticsUpdater.ParseVersion("direkt-sync-v0.2.0.1")>StatisticsUpdater.ParseVersion("0.2.0.0"));
bool reject=false;try{StatisticsUpdater.ParseVersion("1.2.3");}catch(InvalidDataException){reject=true;}Need(reject);
string dir=Path.Combine(args[1],"extracted");StatisticsUpdater.ExtractPackage(File.ReadAllBytes(args[0]),dir,"0.2.0.0");Need(File.Exists(Path.Combine(dir,"Mod_Statistik.dll")));
reject=false;try{StatisticsUpdater.ExtractPackage(File.ReadAllBytes(args[0]),Path.Combine(args[1],"bad-version"),"0.2.0.1");}catch(InvalidDataException){reject=true;}Need(reject);
using(var stream=new MemoryStream()){using(var z=new ZipArchive(stream,ZipArchiveMode.Create,true)){var e=z.CreateEntry("Mod_Statistik/../outside.txt");using(var w=new StreamWriter(e.Open()))w.Write("bad");}reject=false;try{StatisticsUpdater.ExtractPackage(stream.ToArray(),Path.Combine(args[1],"bad-path"),"0.2.0.0");}catch(InvalidDataException){reject=true;}Need(reject);}
StatisticsUpdater.Configure(Path.Combine(args[1],"no-update-config"));StatisticsUpdater.EnsureChecked();Need(!StatisticsUpdater.Press()&&StatisticsUpdater.Caption=="GitHub");
Console.WriteLine("Versionsvergleich, gueltiges ZIP, falsche Version, Pfadausbruch und fehlende Updatequelle bestanden.");return 0;
}catch(Exception e){Console.Error.WriteLine(e);return 1;}}}
