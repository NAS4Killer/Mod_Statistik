using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ModStatistik
{
    internal sealed class SyncSnapshot
    {
        internal readonly BloodmoonLog Data;
        internal readonly string Error;
        internal readonly bool Busy;
        internal SyncSnapshot(BloodmoonLog data, string error, bool busy) { Data = data; Error = error; Busy = busy; }
    }

    internal static class StatisticsSync
    {
        private static string localFile, lastText, error;
        private static object activeWorld;
        private static bool serverMode, roleReady, pendingExtended;
        private static BloodmoonLog data, rawHistory, historical, historicalSource, mergedHistorical, legacySource;
        private static BloodmoonNight sourceLive, mergedLive, remoteLive, remoteFinished;
        private static long historyRevision = -1, liveRevision = -1;
        private static byte[] payload, digest, historyPayload, historyDigest;
        private static int sequence, pendingRequest, protocol;
        private static float deadline, nextRead, nextHudRequest, remoteFinishedAt;
        private static readonly StatisticsTransfer Transfer = new StatisticsTransfer();
        private static readonly Dictionary<ClientInfo, float> Requests = new Dictionary<ClientInfo, float>();
        private static readonly HashSet<ClientInfo> FinalReports = new HashSet<ClientInfo>();

        internal static void Initialize(string modPath)
        {
            localFile = Path.Combine(modPath, "Data", "bloodmoon_kills.txt");
            BloodmoonRecording.Initialize(modPath);
            BloodmoonAccuracyRuntime.Initialize(modPath);
            ModEvents.GameUpdate.RegisterHandler(OnGameUpdate);
        }
        private static void OnGameUpdate(ref ModEvents.SGameUpdateData eventData) { Tick(); }
        internal static bool TryHudValue(string name, out long kills)
        {
            Tick(); kills = 0;
            if (!roleReady) return false;
            return BloodmoonHudDisplay.TryValue(serverMode ? BloodmoonRecording.LiveNight : remoteLive,
                serverMode ? BloodmoonRecording.FinishedNight : remoteFinished,
                serverMode ? BloodmoonRecording.FinishedAt : remoteFinishedAt, Time.unscaledTime, name, out kills);
        }

        internal static bool TryHudAccuracy(string name, out ShotAccuracy accuracy)
        {
            Tick(); accuracy = null;
            if (!roleReady) return false;
            BloodmoonNight finished = serverMode ? BloodmoonRecording.FinishedNight : remoteFinished;
            if (serverMode && finished != null && data != null)
                finished = data.Nights.FirstOrDefault(n => n.Day == finished.Day);
            return BloodmoonHudDisplay.TryAccuracy(serverMode ? mergedLive : remoteLive, finished,
                serverMode ? BloodmoonRecording.FinishedAt : remoteFinishedAt, Time.unscaledTime, name, out accuracy);
        }

        internal static void Tick()
        {
            object world = GameManager.Instance == null ? null : GameManager.Instance.World;
            if (!ReferenceEquals(world, activeWorld))
            {
                BloodmoonAccuracyRuntime.SetWorld(null, false);
                BloodmoonRecording.SetWorld(null, false);
                activeWorld = world; serverMode = false; roleReady = false;
                data = rawHistory = historical = historicalSource = mergedHistorical = legacySource = null;
                sourceLive = mergedLive = null; historyRevision = liveRevision = -1;
                payload = digest = historyPayload = historyDigest = null; error = lastText = null;
                pendingRequest = protocol = 0; pendingExtended = false;
                Transfer.Reset(); Requests.Clear(); FinalReports.Clear(); nextRead = nextHudRequest = 0;
                remoteLive = remoteFinished = null; remoteFinishedAt = 0;
            }
            if (world == null) return;
            if (!roleReady)
            {
                var connection = ConnectionManager.Instance;
                if (!GameManager.IsDedicatedServer && connection == null) return;
                serverMode = GameManager.IsDedicatedServer || connection.IsServer;
                roleReady = true;
                BloodmoonRecording.SetWorld(world as World, serverMode);
                BloodmoonAccuracyRuntime.SetWorld(world as World, serverMode);
            }
            if (serverMode)
            {
                bool wasRecording = BloodmoonRecording.LiveNight != null;
                BloodmoonRecording.Tick();
                if (wasRecording && BloodmoonRecording.LiveNight == null) nextRead = 0;
            }
            BloodmoonAccuracyRuntime.Tick();
            float now = Time.unscaledTime;
            if (serverMode && now >= nextRead)
            {
                nextRead = now + 5f; ReadLocal();
                var clients = ConnectionManager.Instance == null ? null : ConnectionManager.Instance.Clients;
                foreach (var client in Requests.Keys.ToArray())
                    if (clients == null || !clients.Contains(client)) Requests.Remove(client);
                FinalReports.RemoveWhere(client => clients == null || !clients.Contains(client));
            }
            if (serverMode) RefreshLive();
            if (!serverMode && pendingRequest != 0 && now >= deadline)
            {
                pendingRequest = 0; Transfer.Reset();
                if (pendingExtended && protocol == 0)
                {
                    // An old server swallows unknown reserved frames. Try its
                    // existing protocol once; its kill statistics remain usable.
                    protocol = 1; SendRequest();
                }
                else error = "Statistikserver antwortet nicht. Direkt-Sync auf dem Server pruefen.";
            }
            if (!serverMode && pendingRequest == 0 && now >= nextHudRequest &&
                (remoteLive != null || BloodmoonAccuracyRuntime.IsRecording || BloodmoonAccuracyRuntime.NeedsFinalReport))
                SendRequest();
        }

        internal static SyncSnapshot Snapshot()
        { Tick(); return new SyncSnapshot(data, error ?? BloodmoonRecording.Error ?? BloodmoonAccuracyRuntime.Error, pendingRequest != 0); }

        private static void ReadLocal()
        {
            try
            {
                var info = new FileInfo(localFile);
                if (!info.Exists || info.Length > 2 * 1024 * 1024) throw new IOException();
                string text = File.ReadAllText(localFile, new UTF8Encoding(false, true));
                if (rawHistory == null || text != lastText)
                { rawHistory = BloodmoonLog.Parse(text); lastText = text; }
                error = null;
            }
            catch (Exception) { error = "Server-Statistik fehlt oder ist ungueltig. Data/bloodmoon_kills.txt pruefen."; }
        }

        private static BloodmoonNight WithAccuracy(BloodmoonNight night)
        {
            var accuracy = BloodmoonAccuracyRuntime.ForDay(night.Day);
            var kills = night.Kills.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
            // A client can fire between joining and the next kill-counter sample.
            // Retain that valid report even if the client leaves before the sample.
            foreach (var name in accuracy.Keys) if (!kills.ContainsKey(name)) kills.Add(name, 0);
            return new BloodmoonNight(night.Day, kills, accuracy);
        }
        private static void RefreshLive()
        {
            try
            {
                if (rawHistory == null) return;
                long revision = BloodmoonAccuracyRuntime.HistoryRevision;
                if (!ReferenceEquals(rawHistory, historicalSource) || historyRevision != revision)
                {
                    var updated = BloodmoonLog.FromNights(rawHistory.Nights.Select(WithAccuracy));
                    var encoded = StatisticsWire.EncodeAccuracy(updated, 0);
                    historical = updated; historyPayload = encoded; historyDigest = StatisticsWire.Hash(encoded);
                    historicalSource = rawHistory; historyRevision = revision;
                }
                var live = BloodmoonRecording.LiveNight;
                long currentRevision = BloodmoonAccuracyRuntime.Revision;
                if (ReferenceEquals(historical, mergedHistorical) && ReferenceEquals(live, sourceLive) && liveRevision == currentRevision) return;
                mergedLive = live == null ? null : WithAccuracy(live);
                data = mergedLive == null ? historical : BloodmoonLog.FromNights(
                    historical.Nights.Where(n => n.Day != mergedLive.Day).Concat(new[] { mergedLive }));
                mergedHistorical = historical; sourceLive = live; liveRevision = currentRevision;
            }
            catch (Exception) { error = "Laufende Statistik konnte nicht aufbereitet werden; letzte gueltige Werte bleiben erhalten."; }
        }

        internal static void Request()
        {
            Tick();
            if (activeWorld != null && roleReady && !serverMode && pendingRequest == 0 &&
                (protocol != 2 || Time.unscaledTime >= nextHudRequest)) SendRequest();
        }
        private static void SendRequest()
        {
            sequence = sequence == int.MaxValue ? 1 : sequence + 1;
            pendingRequest = sequence; Transfer.Reset(); pendingExtended = protocol != 1;
            deadline = Time.unscaledTime + (protocol == 0 ? 2f : 10f);
            nextHudRequest = Time.unscaledTime + 1f; error = null;
            try
            {
                if (pendingExtended) StatisticsTransport.SendExtendedRequest(sequence, historyDigest, BloodmoonAccuracyRuntime.Report());
                else StatisticsTransport.SendRequest(sequence, digest);
            }
            catch (Exception) { pendingRequest = 0; error = "Statistik konnte nicht angefordert werden."; }
        }

        internal static void FlushAccuracy()
        {
            if (serverMode || !roleReady || protocol == 1 || activeWorld == null) return;
            try
            {
                sequence = sequence == int.MaxValue ? 1 : sequence + 1;
                StatisticsTransport.SendExtendedRequest(sequence, historyDigest, BloodmoonAccuracyRuntime.Report(true));
            }
            catch (Exception e) { Debug.LogWarning("[Mod_Statistik] Abschlussmeldung: " + e.GetType().Name); }
        }

        private static bool AllowRequest(ClientInfo sender, int id)
        {
            Tick();
            if (!Authenticated(sender, id)) return false;
            float last;
            if (Requests.TryGetValue(sender, out last) && Time.unscaledTime - last < 1f) return false;
            Requests[sender] = Time.unscaledTime; return true;
        }

        private static bool Authenticated(ClientInfo sender, int id)
        {
            var manager = ConnectionManager.Instance;
            return serverMode && activeWorld != null && sender != null && sender.entityId > 0 && manager != null &&
                manager.Clients.ForEntityId(sender.entityId) == sender && id > 0;
        }

        internal static void HandleRequest(ClientInfo sender, int id, byte[] knownHash)
        {
            if (!AllowRequest(sender, id)) return;
            if (data == null || error != null)
            { StatisticsTransport.SendReply(sender, id, 2, 0, 0, 0, null, new byte[0]); return; }
            if (!ReferenceEquals(legacySource, data))
            {
                payload = StatisticsWire.Encode(data, mergedLive == null ? 0 : mergedLive.Day);
                digest = StatisticsWire.Hash(payload); legacySource = data;
            }
            if (StatisticsWire.Equal(knownHash, digest))
            { StatisticsTransport.SendReply(sender, id, 1, 0, 0, 0, digest, new byte[0]); return; }
            SendChunks(sender, id, payload, digest, false);
        }

        internal static void HandleExtendedRequest(ClientInfo sender, int id, byte[] knownHash, AccuracyReport report)
        {
            if (report != null && report.Final)
            {
                Tick();
                // One final flush per authenticated connection may arrive immediately
                // after its last regular update. It never bypasses response throttling.
                if (!Authenticated(sender, id)) return;
                if (FinalReports.Add(sender)) BloodmoonAccuracyRuntime.Accept(sender.entityId, report);
            }
            if (!AllowRequest(sender, id)) return;
            if (report == null || !report.Final) BloodmoonAccuracyRuntime.Accept(sender.entityId, report);
            RefreshLive();
            if (historyPayload == null || error != null)
            { StatisticsTransport.SendExtendedReply(sender, id, 2, 0, 0, 0, null, new byte[0]); return; }
            var liveLog = BloodmoonLog.FromNights(mergedLive == null ? new BloodmoonNight[0] : new[] { mergedLive });
            byte[] liveBytes = StatisticsWire.EncodeAccuracy(liveLog, mergedLive == null ? 0 : mergedLive.Day);
            byte[] delta = StatisticsDelta.Encode(historyPayload, historyDigest, liveBytes, !StatisticsWire.Equal(knownHash, historyDigest));
            SendChunks(sender, id, delta, StatisticsWire.Hash(delta), true);
        }

        private static void SendChunks(ClientInfo sender, int id, byte[] bytes, byte[] hash, bool extended)
        {
            int count = (bytes.Length + StatisticsWire.ChunkBytes - 1) / StatisticsWire.ChunkBytes;
            for (int i = 0; i < count; i++)
            {
                byte[] chunk = new byte[Math.Min(StatisticsWire.ChunkBytes, bytes.Length - i * StatisticsWire.ChunkBytes)];
                Buffer.BlockCopy(bytes, i * StatisticsWire.ChunkBytes, chunk, 0, chunk.Length);
                if (extended) StatisticsTransport.SendExtendedReply(sender, id, 0, i, count, bytes.Length, hash, chunk);
                else StatisticsTransport.SendReply(sender, id, 0, i, count, bytes.Length, hash, chunk);
            }
        }

        private static bool ReplyAllowed(int id)
        { Tick(); return !serverMode && activeWorld != null && id == pendingRequest && pendingRequest != 0; }
        private static void SetRemote(BloodmoonLog parsed, BloodmoonNight live)
        {
            var previous = remoteLive; remoteLive = live;
            if (live != null) { remoteFinished = null; remoteFinishedAt = 0; }
            else if (previous != null)
            { remoteFinished = parsed.Nights.FirstOrDefault(n => n.Day == previous.Day); remoteFinishedAt = Time.unscaledTime; }
            else if (remoteFinished != null)
                remoteFinished = parsed.Nights.FirstOrDefault(n => n.Day == remoteFinished.Day);
            data = parsed; error = null; pendingRequest = 0; nextHudRequest = Time.unscaledTime + 1f;
        }
        private static void BadReply()
        { pendingRequest = 0; Transfer.Reset(); error = "Ungueltige Statistikdaten empfangen; letzte gueltige Werte bleiben erhalten."; }

        internal static void HandleExtendedReply(int id, byte result, int index, int count, int total, byte[] hash, byte[] chunk)
        {
            if (!ReplyAllowed(id)) return;
            try
            {
                if (result == 2) { pendingRequest = 0; Transfer.Reset(); error = "Server-Statistik konnte nicht gelesen werden."; return; }
                if (result != 0) throw new InvalidDataException();
                byte[] completed = Transfer.Accept(id, index, count, total, hash, chunk);
                if (completed == null) return;
                var delta = StatisticsDelta.Decode(completed);
                int activeDay;
                BloodmoonLog newHistory;
                if (delta.HistoryPayload != null)
                {
                    newHistory = StatisticsWire.Decode(delta.HistoryPayload, out activeDay);
                    if (activeDay != 0) throw new InvalidDataException();
                }
                else
                {
                    if (historical == null || !StatisticsWire.Equal(delta.HistoryHash, historyDigest)) throw new InvalidDataException();
                    newHistory = historical;
                }
                var liveData = StatisticsWire.Decode(delta.LivePayload, out activeDay);
                if (liveData.Nights.Count != (activeDay == 0 ? 0 : 1)) throw new InvalidDataException();
                var live = activeDay == 0 ? null : liveData.Nights[0];
                var combined = live == null ? newHistory : BloodmoonLog.FromNights(newHistory.Nights.Where(n => n.Day != live.Day).Concat(new[] { live }));
                historical = newHistory; historyDigest = delta.HistoryHash; protocol = 2;
                SetRemote(combined, live);
            }
            catch (Exception) { BadReply(); }
        }

        internal static void HandleReply(int id, byte result, int index, int count, int total, byte[] hash, byte[] chunk)
        {
            if (!ReplyAllowed(id)) return;
            try
            {
                if (result == 2) { pendingRequest = 0; Transfer.Reset(); error = "Die Statistikdatei auf dem Server fehlt oder ist ungueltig."; return; }
                if (result == 1)
                {
                    if (data == null || !StatisticsWire.Equal(digest, hash)) throw new InvalidDataException();
                    pendingRequest = 0; Transfer.Reset(); error = null; protocol = 1; nextHudRequest = Time.unscaledTime + 1f; return;
                }
                byte[] completed = Transfer.Accept(id, index, count, total, hash, chunk);
                if (completed == null) return;
                int activeDay;
                var parsed = StatisticsWire.Decode(completed, out activeDay);
                SetRemote(parsed, activeDay == 0 ? null : parsed.Nights.Single(n => n.Day == activeDay));
                digest = StatisticsWire.Hash(completed); protocol = 1;
            }
            catch (Exception) { BadReply(); }
        }
    }
}
