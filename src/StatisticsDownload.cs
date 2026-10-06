using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ModStatistik
{
    public sealed class StatisticsDownload
    {
        public const string Version = "0.0.0.9";
        public const string DefaultUrl = "";
        private const int MaximumBytes = 2 * 1024 * 1024;
        private readonly string directory;
        private readonly object gate = new object();
        private readonly string localFile;
        public string Url { get; private set; }
        public string LastError { get; private set; }
        public DateTime? LastSuccess { get; private set; }
        public BloodmoonLog Data { get; private set; }
        public bool Busy { get; private set; }
        public bool IsLocal { get; private set; }
        public DateTime? LocalFileTime { get; private set; }

        public StatisticsDownload(string directory, string localFile = null)
        {
            this.directory = directory;
            this.localFile = localFile;
            Url = DefaultUrl;
            try
            {
                string settings = Path.Combine(directory, "download-url.txt");
                if (File.Exists(settings)) Url = ValidateUrl(File.ReadAllText(settings, Encoding.UTF8).Trim());
                LoadCache();
            }
            catch (Exception) { LastError = "Gespeicherte Einstellungen oder Statistik konnten nicht gelesen werden."; }
            if (Data == null && localFile != null) LoadLocal();
        }

        public StatisticsSnapshot Snapshot()
        {
            lock (gate) { return new StatisticsSnapshot(Url, LastError, LastSuccess, Data, Busy, IsLocal, LocalFileTime); }
        }

        public void LoadLocal()
        {
            lock (gate)
            {
                if (Busy) throw new InvalidOperationException("Bitte den laufenden Abruf abwarten.");
                try
                {
                    if (localFile == null || !File.Exists(localFile)) throw new IOException("Lokale TXT fehlt.");
                    var info = new FileInfo(localFile);
                    if (info.Length > MaximumBytes) throw new IOException("Lokale TXT zu gross.");
                    var parsed = BloodmoonLog.Parse(File.ReadAllText(localFile, new UTF8Encoding(false, true)));
                    Data = parsed;
                    IsLocal = true;
                    LocalFileTime = info.LastWriteTime;
                    LastSuccess = null;
                    LastError = null;
                }
                catch (Exception) { LastError = "Lokale TXT fehlt oder ist ungueltig. Data/bloodmoon_kills.txt im Mod-Ordner pruefen."; }
            }
        }

        public static string ValidateUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            Uri uri;
            if (value == null || value.Length > 2048 || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) || uri.UserInfo.Length > 0 || uri.Fragment.Length > 0)
                throw new ArgumentException("Bitte eine HTTP- oder HTTPS-Adresse ohne Zugangsdaten eingeben.");
            return uri.AbsoluteUri;
        }

        public void SaveUrl(string value)
        {
            string url = ValidateUrl(value);
            lock (gate)
            {
                if (Busy) throw new InvalidOperationException("Bitte den laufenden Abruf abwarten.");
                AtomicWrite(Path.Combine(directory, "download-url.txt"), url + "\n");
                if (url != Url) { Data = null; LastSuccess = null; IsLocal = false; LocalFileTime = null; }
                Url = url;
                LastError = null;
                LoadCache();
                if (url.Length == 0 && localFile != null) LoadLocal();
            }
        }

        private void LoadCache()
        {
            if (Url.Length == 0) return;
            string cacheUrl = Path.Combine(directory, "cache-url.txt");
            string cache = Path.Combine(directory, "bloodmoon_kills.txt");
            if (!File.Exists(cacheUrl) || !File.Exists(cache) || File.ReadAllText(cacheUrl, Encoding.UTF8).Trim() != Url) return;
            var info = new FileInfo(cache);
            if (info.Length > MaximumBytes) throw new IOException("Cache zu gross.");
            Data = BloodmoonLog.Parse(File.ReadAllText(cache, new UTF8Encoding(false, true)));
            LastSuccess = info.LastWriteTime;
            IsLocal = false;
        }

        public Task RefreshAsync()
        {
            string url;
            lock (gate)
            {
                if (Busy) return Task.FromResult(false);
                if (Url.Length == 0) { LoadLocal(); return Task.FromResult(false); }
                Busy = true;
                url = Url;
            }
            return Task.Run(delegate
            {
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.Timeout = 10000;
                    request.ReadWriteTimeout = 10000;
                    request.AllowAutoRedirect = false;
                    request.UserAgent = "Mod_Statistik/" + Version;
                    string text;
                    using (var response = (HttpWebResponse)request.GetResponse())
                    using (var stream = response.GetResponseStream())
                    using (var memory = new MemoryStream())
                    {
                        if (response.StatusCode != HttpStatusCode.OK) throw new WebException("HTTP-Fehler.");
                        if (response.ContentLength > MaximumBytes) throw new IOException("Download zu gross.");
                        var buffer = new byte[8192];
                        int count;
                        while ((count = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            if (memory.Length + count > MaximumBytes) throw new IOException("Download zu gross.");
                            memory.Write(buffer, 0, count);
                        }
                        text = new UTF8Encoding(false, true).GetString(memory.ToArray()).TrimStart('\uFEFF');
                    }
                    BloodmoonLog parsed = BloodmoonLog.Parse(text);
                    lock (gate)
                    {
                        // Invalidate the old source association before replacing either file.
                        AtomicWrite(Path.Combine(directory, "cache-url.txt"), "\n");
                        AtomicWrite(Path.Combine(directory, "bloodmoon_kills.txt"), text);
                        AtomicWrite(Path.Combine(directory, "cache-url.txt"), url + "\n");
                        Data = parsed;
                        IsLocal = false;
                        LocalFileTime = null;
                        LastSuccess = DateTime.Now;
                        LastError = null;
                    }
                }
                catch (Exception e)
                {
                    lock (gate)
                    {
                        LastError = e is FormatException ? "Die heruntergeladene TXT-Datei hat kein gueltiges Blutmond-Format." :
                            e is WebException ? "Download fehlgeschlagen. Adresse und Verbindung pruefen." : "Statistik konnte nicht geladen oder gespeichert werden.";
                    }
                }
                finally { lock (gate) { Busy = false; } }
            });
        }

        private void AtomicWrite(string path, string text)
        {
            Directory.CreateDirectory(directory);
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, text, new UTF8Encoding(false));
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }

    public sealed class StatisticsSnapshot
    {
        public readonly string Url;
        public readonly string LastError;
        public readonly DateTime? LastSuccess;
        public readonly BloodmoonLog Data;
        public readonly bool Busy;
        public readonly bool IsLocal;
        public readonly DateTime? LocalFileTime;
        internal StatisticsSnapshot(string url, string error, DateTime? success, BloodmoonLog data, bool busy, bool local, DateTime? fileTime)
        {
            Url = url; LastError = error; LastSuccess = success; Data = data;
            Busy = busy; IsLocal = local; LocalFileTime = fileTime;
        }
    }
}
