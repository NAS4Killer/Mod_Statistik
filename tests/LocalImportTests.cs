using System;
using System.IO;
using ModStatistik;

class LocalImportTests
{
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    static void Main(string[] args)
    {
        Directory.CreateDirectory(args[1]);
        string file = Path.Combine(args[1], "local-source.txt");
        File.WriteAllText(file, File.ReadAllText(args[0]));
        var client = new StatisticsDownload(Path.Combine(args[1], "preferences"), file);
        Require(client.Snapshot().Url == "" && client.Snapshot().IsLocal, "Lokale TXT ohne Serveradresse laden.");
        Require(client.Snapshot().Data.Nights[0].Total == 385, "Lokale Nachtwerte.");
        var previous = client.Snapshot().Data;
        File.WriteAllText(file, "ungueltiger Inhalt");
        client.LoadLocal();
        Require(client.Snapshot().Data == previous && client.Snapshot().LastError != null, "Ungueltige lokale Datei darf die Anzeige nicht loeschen.");
        File.WriteAllText(file, File.ReadAllText(args[0]));
        client.RefreshAsync().Wait();
        Require(client.Snapshot().LastError == null && client.Snapshot().IsLocal, "Aktualisieren mit leerer URL liest lokal.");
        client.SaveUrl(" ");
        Require(client.Snapshot().Url == "" && client.Snapshot().IsLocal, "Leere URL speichern.");
        Console.WriteLine("Lokaler Start, fehlerhafter Import, Aktualisieren und leere URL geprueft.");
    }
}
