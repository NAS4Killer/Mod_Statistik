using System;
using System.IO;
using ModStatistik;

class DownloadTests
{
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    static void Main(string[] args)
    {
        var client = new StatisticsDownload(args[1]);
        client.SaveUrl(args[0] + "/good");
        client.RefreshAsync().Wait();
        Require(client.LastError == null && client.Data.Nights[0].Total == 385, "Download und Parsing.");
        var restored = new StatisticsDownload(args[1]);
        Require(restored.Data.Nights[0].Total == 385 && restored.LastSuccess.HasValue, "Cache nach Neustart.");
        restored.SaveUrl(args[0] + "/bad");
        Require(restored.Data == null && restored.LastSuccess == null, "URL-Wechsel muss die alte Quelle ausblenden.");
        restored.RefreshAsync().Wait();
        Require(restored.LastError != null && restored.Data == null, "HTML-Fehlerseite abweisen.");
        restored.SaveUrl(args[0] + "/good");
        Require(restored.Data.Nights[0].Total == 385, "Gueltiger Cache muss erhalten bleiben.");
        var failed = new StatisticsDownload(args[1]);
        failed.SaveUrl(args[0] + "/missing");
        failed.RefreshAsync().Wait();
        Require(failed.LastError != null && !failed.Busy, "HTTP-Fehler sichtbar, Abruf freigegeben.");
        Console.WriteLine("Download, Cache-Neustart, Quellenwechsel, HTML-Fehler und HTTP-Fehler geprueft.");
    }
}
