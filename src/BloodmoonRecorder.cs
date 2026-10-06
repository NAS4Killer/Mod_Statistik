using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace ModStatistik
{
    internal sealed class KillSample
    {
        internal readonly string Id, Name;
        internal readonly long Total;
        internal KillSample(string id, string name, long total) { Id = id; Name = name; Total = total; }
    }

    internal sealed class BloodmoonRecorder
    {
        [DataContract] internal sealed class PlayerState
        {
            [DataMember] public string Id, Name;
            [DataMember] public long Start, Last, Kills;
        }
        [DataContract] internal sealed class RecorderState
        {
            [DataMember] public int Version = 1, Day;
            [DataMember] public ulong StartTime, EndTime;
            [DataMember] public bool Active;
            [DataMember] public List<PlayerState> Players = new List<PlayerState>();
        }
        private readonly string logFile, stateFile;
        private RecorderState state;
        private float nextSave;
        private bool dirty;
        private BloodmoonNight live;
        internal BloodmoonNight LiveNight { get { if (!state.Active) return null; if (live == null) live = new BloodmoonNight(state.Day, state.Players.ToDictionary(p => p.Name, p => p.Kills, StringComparer.Ordinal)); return live; } }
        private HashSet<int> completed;
        internal bool Active { get { return state.Active; } }

        internal BloodmoonRecorder(string directory, string worldKey)
        {
            Directory.CreateDirectory(directory);
            logFile = Path.Combine(directory, "bloodmoon_kills.txt");
            stateFile = Path.Combine(directory, "bloodmoon_state-" + worldKey + ".json");
            EnsureLog();
            completed = new HashSet<int>(ReadLog().Nights.Select(n => n.Day));
            state = new RecorderState();
            if (File.Exists(stateFile))
            {
                if (new FileInfo(stateFile).Length > 256 * 1024) throw new InvalidDataException("Zwischenstand zu gross.");
                using (var input = File.OpenRead(stateFile))
                    state = (RecorderState)new DataContractJsonSerializer(typeof(RecorderState)).ReadObject(input);
                if (state == null || state.Version != 1 || state.Players == null || state.Players.Count > 128 ||
                    (state.Active && (state.Day < 1 || state.EndTime <= state.StartTime)) ||
                    state.Players.Any(p => p == null || string.IsNullOrEmpty(p.Id) || !ValidName(p.Name) || p.Start < 0 || p.Last < 0 || p.Kills < 0) ||
                    state.Players.Select(p => p.Id).Distinct().Count() != state.Players.Count)
                    throw new InvalidDataException("Zwischenstand ungueltig.");
            }
        }

        // Sampling is deferred: the caller's player counters are read only inside a bloodmoon.
        internal void Step(ulong now, int plannedDay, int dusk, int dawn, float realTime, Func<IEnumerable<KillSample>> sample)
        {
            if (!state.Active)
            {
                if (plannedDay <= 0 || dusk < 0 || dusk > 23 || dawn < 0 || dawn > 23) return;
                ulong start = (ulong)(plannedDay - 1) * 24000UL + (ulong)dusk * 1000UL;
                ulong end = (ulong)plannedDay * 24000UL + (ulong)dawn * 1000UL;
                if (now < start || now >= end || Completed(plannedDay)) return;
                state = new RecorderState { Active = true, Day = plannedDay, StartTime = start, EndTime = end };
                Sample(sample());
                Save();
                Append("=== Blutmond Tag " + state.Day + " START " + Stamp(now) + " ===\n" +
                    string.Join("\n", state.Players.OrderBy(p => p.Name, StringComparer.Ordinal).Select(p => p.Name + " start_zombies=" + p.Start)) + "\n\n");
                nextSave = realTime + 5;
                return;
            }
            // A time rollback must not silently close a running night.
            if (now < state.StartTime) return;
            Sample(sample());
            if (now >= state.EndTime)
            {
                EnsureLog();
                completed = new HashSet<int>(ReadLog().Nights.Select(n => n.Day));
                if (!Completed(state.Day))
                {
                    string rows = string.Join("\n", state.Players.OrderBy(p => p.Name, StringComparer.Ordinal)
                        .Select(p => p.Name + " start=" + p.Start + " end=" + p.Last + " bm_kills=" + p.Kills));
                    Append("=== Blutmond Tag " + state.Day + " END " + Stamp(now) + " ===\n" +
                        (rows.Length == 0 ? "(keine Spieler)" : rows) + "\n\n");
                }
                completed.Add(state.Day);
                state.Active = false; dirty = true; Save();
            }
            else if (dirty && realTime >= nextSave) { Save(); nextSave = realTime + 5; }
        }

        internal void Capture(IEnumerable<KillSample> samples) { if (state.Active) { Sample(samples); Save(); } }
        internal void Flush() { if (dirty) Save(); }

        private void Sample(IEnumerable<KillSample> samples)
        {
            foreach (var item in samples)
            {
                if (item == null || string.IsNullOrEmpty(item.Id) || !ValidName(item.Name) || item.Total < 0) continue;
                var player = state.Players.FirstOrDefault(p => p.Id == item.Id);
                if (player == null)
                {
                    if (state.Players.Count >= 128) throw new InvalidDataException("Zu viele Spieler.");
                    if (state.Players.Any(p => p.Name == item.Name)) throw new InvalidDataException("Doppelter Spielername.");
                    player = new PlayerState { Id = item.Id, Name = item.Name, Start = item.Total, Last = item.Total };
                    state.Players.Add(player); dirty = true; live = null;
                }
                // A reset does not subtract previously recorded kills.
                if (item.Total > player.Last) player.Kills = checked(player.Kills + item.Total - player.Last);
                if (item.Total != player.Last) { player.Last = item.Total; dirty = true; live = null; }
            }
        }

        private static bool ValidName(string name) { return !string.IsNullOrWhiteSpace(name) && name.Length <= 16 && !name.Any(char.IsControl); }
        private static string Stamp(ulong now)
        {
            return DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture) + " (Ingame Tag " +
                (now / 24000UL + 1) + " - " + ((now % 24000UL) / 1000UL).ToString("00") + ":" +
                ((now % 1000UL) * 60UL / 1000UL).ToString("00") + ")";
        }
        private void EnsureLog()
        {
            if (!File.Exists(logFile)) { AtomicWrite(logFile, Encoding.UTF8.GetBytes("=== Blutmond Tag 0 INIT " + DateTime.Now.ToString("s") + " ===\n\n")); return; }
            try { ReadLog(); }
            catch (FormatException) { BackupBadLog(); }
            catch (DecoderFallbackException) { BackupBadLog(); }
        }
        private void BackupBadLog()
        {
            string backup = logFile + ".invalid-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N") + ".bak";
            File.Copy(logFile, backup, false);
            AtomicWrite(logFile, Encoding.UTF8.GetBytes("=== Blutmond Tag 0 INIT " + DateTime.Now.ToString("s") + " ===\n\n"));
        }
        private string ReadText()
        {
            if (new FileInfo(logFile).Length > 2 * 1024 * 1024) throw new IOException("Protokoll zu gross.");
            return File.ReadAllText(logFile, new UTF8Encoding(false, true));
        }
        private BloodmoonLog ReadLog() { return BloodmoonLog.Parse(ReadText()); }
        private bool Completed(int day) { return completed.Contains(day); }
        private void Append(string text)
        {
            EnsureLog();
            string combined = ReadText() + text;
            BloodmoonLog.Parse(combined);
            byte[] bytes = new UTF8Encoding(false, true).GetBytes(combined);
            if (bytes.Length > 2 * 1024 * 1024) throw new IOException("Protokoll zu gross.");
            AtomicWrite(logFile, bytes);
        }
        private void Save()
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(RecorderState)).WriteObject(stream, state);
                AtomicWrite(stateFile, stream.ToArray());
            }
            dirty = false;
        }
        private static void AtomicWrite(string path, byte[] bytes)
        {
            string temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { output.Write(bytes, 0, bytes.Length); output.Flush(true); }
                if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
