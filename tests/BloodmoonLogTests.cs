using System;
using System.IO;
using System.Linq;
using ModStatistik;

class BloodmoonLogTests
{
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    static void Main(string[] args)
    {
        BloodmoonLog actual = BloodmoonLog.Parse(File.ReadAllText(args[0]));
        Require(actual.Nights.Count == 2, "Doppelte Nacht muss ignoriert werden.");
        Require(actual.Nights[0].Day == 84 && actual.Nights[0].Total == 385, "Tag 84 muss 385 Kills behalten.");
        Require(actual.Nights[1].Day == 77 && actual.Nights[1].Total == 274, "Tag 77 muss 274 Kills enthalten.");
        Require(!actual.Nights[0].Kills.ContainsKey("Player5"), "Fehlender Spieler darf nicht als Nullwert erfunden werden.");
        Require(actual.Nights[1].Kills["Player2"] == 0, "Vorhandener Nullwert muss erhalten bleiben.");
        var sample = BloodmoonLog.Parse("=== Blutmond Tag 7 END test ===\nEntityPlayer start=0 end=99 bm_kills=99\nA start=0 end=3 bm_kills=3\n=== Blutmond Tag 14 START test ===\nA start_zombies=3\n");
        Require(sample.Nights.Count == 1 && sample.Nights[0].Total == 3, "EntityPlayer und laufende Nacht ausschliessen.");
        bool rejected = false;
        try { BloodmoonLog.Parse("<html>404</html>"); } catch (FormatException) { rejected = true; }
        Require(rejected, "Fehlerseite darf keinen gueltigen Cache ersetzen.");
        rejected = false;
        try { StatisticsDownload.ValidateUrl("https://user:secret@example.invalid/file.txt"); } catch (ArgumentException) { rejected = true; }
        Require(rejected, "Keine Zugangsdaten in Download-URLs.");
        Console.WriteLine("TXT-Auswertung: Tag 84 = 385, Tag 77 = 274; Duplikate, fehlende Werte, Nullwerte und Fehlerseiten geprueft.");
    }
}
