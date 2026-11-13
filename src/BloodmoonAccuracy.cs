using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace ModStatistik
{
    public sealed class ShotAccuracy
    {
        public long Shots { get; private set; }
        public long Hits { get; private set; }
        public string Percentage
        {
            get { return Shots == 0 ? "\u2014" : (100m * Hits / Shots).ToString("0.#", CultureInfo.GetCultureInfo("de-DE")) + " %"; }
        }

        public ShotAccuracy(long shots, long hits)
        {
            if (shots < 0 || hits < 0 || hits > shots) throw new ArgumentOutOfRangeException("hits", "Ungueltige Schuss- oder Trefferzahl.");
            Shots = shots; Hits = hits;
        }
    }

    internal sealed class AccuracyReport
    {
        internal int Day;
        internal string Session;
        internal long Shots, Hits;
        internal bool Final;
    }

    // Separate from bloodmoon_kills.txt so existing history readers keep their format.
    // Reports contain cumulative counters; accepting the same report twice adds nothing.
    internal sealed class BloodmoonAccuracyStore
    {
        internal const int MaximumNights = 512;
        internal const int MaximumPlayers = 128;
        internal const int MaximumSessionsPerPlayer = 64;
        internal const int MaximumSessions = 65536;
        internal const int MaximumFileBytes = 16 * 1024 * 1024;
        internal const long MaximumShots = 1000000000L;

        [DataContract] private sealed class SavedState
        {
            [DataMember] public int Version = 1;
            [DataMember] public List<SavedNight> Nights = new List<SavedNight>();
        }
        [DataContract] private sealed class SavedNight
        {
            [DataMember] public int Day;
            [DataMember] public List<SavedPlayer> Players = new List<SavedPlayer>();
            internal Dictionary<string, SavedPlayer> ById;
        }
        [DataContract] private sealed class SavedPlayer
        {
            [DataMember] public string Id, Name;
            [DataMember] public List<SavedSession> Sessions = new List<SavedSession>();
            internal Dictionary<string, SavedSession> BySession;
            internal long Shots, Hits;
        }
        [DataContract] private sealed class SavedSession
        {
            [DataMember] public string Id;
            [DataMember] public long Shots, Hits;
        }

        private readonly string path;
        private SavedState state = new SavedState();
        private readonly Dictionary<int, SavedNight> byDay = new Dictionary<int, SavedNight>();
        private int sessionCount;
        private bool dirty;
        internal long Revision { get; private set; }

        internal BloodmoonAccuracyStore(string directory, string worldKey)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Datenordner fehlt.", "directory");
            if (string.IsNullOrEmpty(worldKey) || worldKey.Length > 128 ||
                worldKey.Any(c => !((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                    (c >= '0' && c <= '9') || c == '-' || c == '_')))
                throw new ArgumentException("Ungueltige Weltkennung.", "worldKey");
            path = Path.Combine(directory, "bloodmoon_accuracy-" + worldKey + ".json");
            if (!File.Exists(path)) return;
            try
            {
                using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (input.Length <= 0 || input.Length > MaximumFileBytes) throw Invalid("Ungueltige Dateigroesse.");
                    state = (SavedState)Serializer().ReadObject(input);
                }
                IndexAndValidate();
            }
            catch (InvalidDataException) { throw; }
            catch (SerializationException e) { throw new InvalidDataException("Trefferhistorie ist ungueltig; vorhandene Datei bleibt erhalten.", e); }
            catch (System.Xml.XmlException e) { throw new InvalidDataException("Trefferhistorie ist ungueltig; vorhandene Datei bleibt erhalten.", e); }
        }

        internal bool Apply(string playerId, string playerName, AccuracyReport report)
        {
            ValidatePlayer(playerId, playerName);
            if (report == null || report.Day <= 0) throw Invalid("Ungueltiger Blutmondtag.");
            string sessionId = NormalizeSession(report.Session);
            ValidateCounters(report.Shots, report.Hits);

            SavedNight night;
            bool newNight = !byDay.TryGetValue(report.Day, out night);
            if (newNight)
            {
                if (byDay.Count >= MaximumNights) throw Invalid("Zu viele Blutmondnaechte.");
                night = new SavedNight { Day = report.Day, ById = new Dictionary<string, SavedPlayer>(StringComparer.Ordinal) };
            }
            SavedPlayer player;
            bool newPlayer = !night.ById.TryGetValue(playerId, out player);
            if (newPlayer)
            {
                if (night.Players.Count >= MaximumPlayers) throw Invalid("Zu viele Spieler.");
                player = new SavedPlayer { Id = playerId, Name = playerName, BySession = new Dictionary<string, SavedSession>(StringComparer.Ordinal) };
            }
            SavedSession session;
            bool newSession = !player.BySession.TryGetValue(sessionId, out session);
            // Firearm reports contain completed shots and their hit result
            // together. A new successful shot therefore needs a new shot too.
            if (!newSession && (report.Shots < session.Shots || report.Hits < session.Hits ||
                report.Hits - session.Hits > report.Shots - session.Shots)) return false;
            if (night.Players.Any(p => p.Id != playerId && p.Name == playerName)) throw Invalid("Doppelter Spielername.");

            long shots = player.Shots + report.Shots - (newSession ? 0 : session.Shots);
            long hits = player.Hits + report.Hits - (newSession ? 0 : session.Hits);
            ValidateCounters(shots, hits);
            bool changed = newPlayer || player.Name != playerName || player.Shots != shots || player.Hits != hits;
            if (!changed) return false;

            // An empty reconnect does not need another saved session. Its later first
            // nonzero report is still added exactly once.
            bool saveSession = !newSession || newPlayer || report.Shots != 0;
            if (newSession && saveSession && (player.Sessions.Count >= MaximumSessionsPerPlayer || sessionCount >= MaximumSessions))
                throw Invalid("Zu viele Zaehlsitzungen.");
            if (newNight) { byDay.Add(report.Day, night); state.Nights.Add(night); }
            if (newPlayer) { night.ById.Add(playerId, player); night.Players.Add(player); }
            if (saveSession)
            {
                if (newSession)
                {
                    session = new SavedSession { Id = sessionId };
                    player.Sessions.Add(session); player.BySession.Add(sessionId, session); sessionCount++;
                }
                session.Shots = report.Shots; session.Hits = report.Hits;
            }
            player.Name = playerName; player.Shots = shots; player.Hits = hits;
            dirty = true; Revision++;
            return true;
        }

        internal Dictionary<string, ShotAccuracy> ForDay(int day)
        {
            var result = new Dictionary<string, ShotAccuracy>(StringComparer.Ordinal);
            SavedNight night;
            if (byDay.TryGetValue(day, out night))
                foreach (var player in night.Players) result.Add(player.Name, new ShotAccuracy(player.Shots, player.Hits));
            return result;
        }

        internal void Flush()
        {
            if (!dirty) return;
            byte[] bytes;
            using (var memory = new MemoryStream())
            {
                Serializer().WriteObject(memory, state);
                if (memory.Length > MaximumFileBytes) throw Invalid("Trefferhistorie ist zu gross.");
                bytes = memory.ToArray();
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { output.Write(bytes, 0, bytes.Length); output.Flush(true); }
                if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
                dirty = false;
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        private void IndexAndValidate()
        {
            if (state == null || state.Version != 1 || state.Nights == null || state.Nights.Count > MaximumNights)
                throw Invalid("Ungueltige Trefferhistorie.");
            foreach (var night in state.Nights)
            {
                if (night == null || night.Day <= 0 || byDay.ContainsKey(night.Day) ||
                    night.Players == null || night.Players.Count > MaximumPlayers) throw Invalid("Ungueltiger Blutmondtag.");
                night.ById = new Dictionary<string, SavedPlayer>(StringComparer.Ordinal);
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var player in night.Players)
                {
                    if (player == null) throw Invalid("Spieler fehlt.");
                    ValidatePlayer(player.Id, player.Name);
                    if (night.ById.ContainsKey(player.Id) || !names.Add(player.Name) || player.Sessions == null ||
                        player.Sessions.Count == 0 || player.Sessions.Count > MaximumSessionsPerPlayer) throw Invalid("Ungueltiger Spieler.");
                    player.BySession = new Dictionary<string, SavedSession>(StringComparer.Ordinal);
                    foreach (var session in player.Sessions)
                    {
                        if (session == null) throw Invalid("Zaehlsitzung fehlt.");
                        session.Id = NormalizeSession(session.Id);
                        ValidateCounters(session.Shots, session.Hits);
                        if (player.BySession.ContainsKey(session.Id) || ++sessionCount > MaximumSessions) throw Invalid("Ungueltige Zaehlsitzungen.");
                        player.Shots += session.Shots; player.Hits += session.Hits;
                        ValidateCounters(player.Shots, player.Hits);
                        player.BySession.Add(session.Id, session);
                    }
                    night.ById.Add(player.Id, player);
                }
                byDay.Add(night.Day, night);
            }
        }

        private static void ValidatePlayer(string id, string name)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 256 || id.Any(char.IsControl) ||
                string.IsNullOrWhiteSpace(name) || name.Length > 16 || name.Any(char.IsControl)) throw Invalid("Ungueltige Spielerkennung oder Name.");
        }
        private static string NormalizeSession(string text)
        {
            Guid parsed;
            if (text == null || text.Length != 32 || !Guid.TryParseExact(text, "N", out parsed) || parsed == Guid.Empty)
                throw Invalid("Ungueltige Zaehlsitzung.");
            return parsed.ToString("N");
        }
        private static void ValidateCounters(long shots, long hits)
        {
            if (shots < 0 || shots > MaximumShots || hits < 0 || hits > shots) throw Invalid("Ungueltige Schuss- oder Trefferzahl.");
        }
        private static DataContractJsonSerializer Serializer()
        {
            return new DataContractJsonSerializer(typeof(SavedState), new DataContractJsonSerializerSettings { MaxItemsInObjectGraph = 2000000 });
        }
        private static InvalidDataException Invalid(string message) { return new InvalidDataException(message); }
    }
}
