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
        private static string localFile;
        private static object activeWorld;
        private static bool serverMode;
        private static BloodmoonLog data, historical, mergedHistorical;
        private static BloodmoonNight mergedLive;
        private static byte[] payload, digest;
        private static string error;
        private static int sequence, pendingRequest;
        private static float deadline, nextRead;
        private static readonly StatisticsTransfer Transfer = new StatisticsTransfer();
        private static readonly Dictionary<ClientInfo, float> Requests = new Dictionary<ClientInfo, float>();

        internal static void Initialize(string modPath)
        {
            localFile = Path.Combine(modPath, "Data", "bloodmoon_kills.txt");
            BloodmoonRecording.Initialize(modPath);
            ModEvents.GameUpdate.RegisterHandler(OnGameUpdate);
        }

        private static void OnGameUpdate(ref ModEvents.SGameUpdateData eventData) { Tick(); }

        private static bool IsServer
        {
            get { return serverMode; }
        }

        internal static void Tick()
        {
            object world = GameManager.Instance == null ? null : GameManager.Instance.World;
            if (!ReferenceEquals(world, activeWorld))
            {
                activeWorld = world;
                serverMode = world != null && (ConnectionManager.Instance == null || ConnectionManager.Instance.IsServer);
                BloodmoonRecording.SetWorld(world as World, serverMode);
                data = null; historical = null; mergedHistorical = null; mergedLive = null; payload = null; digest = null; error = null;
                pendingRequest = 0; Transfer.Reset(); Requests.Clear(); nextRead = 0;
            }
            if (world == null) return;
            bool server = serverMode;
            if (server)
            {
                bool wasRecording = BloodmoonRecording.LiveNight != null;
                BloodmoonRecording.Tick();
                if (wasRecording && BloodmoonRecording.LiveNight == null) nextRead = 0;
            }
            float now = Time.unscaledTime;
            if (server && now >= nextRead)
            {
                nextRead = now + 5f;
                ReadLocal();
                var clients = ConnectionManager.Instance == null ? null : ConnectionManager.Instance.Clients;
                foreach (var client in Requests.Keys.ToArray())
                    if (clients == null || !clients.Contains(client)) Requests.Remove(client);
            }
            if (server) RefreshLive();
            if (!server && pendingRequest != 0 && now >= deadline)
            {
                pendingRequest = 0; Transfer.Reset();
                error = "Statistikserver antwortet nicht. Direkt-Sync auf dem Server pruefen.";
            }
        }

        internal static SyncSnapshot Snapshot() { Tick(); return new SyncSnapshot(data, error ?? BloodmoonRecording.Error, pendingRequest != 0); }

        private static void ReadLocal()
        {
            try
            {
                var info = new FileInfo(localFile);
                if (!info.Exists || info.Length > 2 * 1024 * 1024) throw new IOException();
                var parsed = BloodmoonLog.Parse(File.ReadAllText(localFile, new UTF8Encoding(false, true)));
                historical = parsed;
                error = null;
                RefreshLive();
            }
            catch (Exception) { error = "Server-Statistik fehlt oder ist ungueltig. Data/bloodmoon_kills.txt pruefen."; }
        }

        private static void RefreshLive()
        {
            try { RefreshLiveCore(); }
            catch (Exception) { error = "Laufende Statistik konnte nicht aufbereitet werden; letzte gueltige Werte bleiben erhalten."; }
        }
        private static void RefreshLiveCore()
        {
            var live = BloodmoonRecording.LiveNight;
            if (historical == null || (ReferenceEquals(historical, mergedHistorical) && ReferenceEquals(live, mergedLive))) return;
            var combined = live == null ? historical : BloodmoonLog.FromNights(historical.Nights.Where(n => n.Day != live.Day).Concat(new[] { live }));
            byte[] encoded = StatisticsWire.Encode(combined);
            byte[] hash = StatisticsWire.Hash(encoded);
            if (!StatisticsWire.Equal(hash, digest)) { data = combined; payload = encoded; digest = hash; }
            mergedHistorical = historical; mergedLive = live;
        }

        internal static void Request()
        {
            Tick();
            if (activeWorld == null || IsServer || pendingRequest != 0) return;
            sequence = sequence == int.MaxValue ? 1 : sequence + 1;
            pendingRequest = sequence; Transfer.Reset(); deadline = Time.unscaledTime + 10f; error = null;
            try
            {
                ConnectionManager.Instance.SendToServer(NetPackageManager.GetPackage<NetPackageStatisticsRequest>().Setup(sequence, digest), false);
            }
            catch (Exception)
            {
                pendingRequest = 0;
                error = "Statistik konnte nicht angefordert werden.";
            }
        }

        internal static void HandleRequest(ClientInfo sender, int id, byte[] knownHash)
        {
            Tick();
            var manager = ConnectionManager.Instance;
            if (!IsServer || activeWorld == null || sender == null || sender.entityId <= 0 || manager == null ||
                manager.Clients.ForEntityId(sender.entityId) != sender || id <= 0) return;
            float last;
            if (Requests.TryGetValue(sender, out last) && Time.unscaledTime - last < 1f) return;
            Requests[sender] = Time.unscaledTime;
            if (payload == null || error != null)
            {
                sender.SendPackage(NetPackageManager.GetPackage<NetPackageStatisticsReply>().Setup(id, 2, 0, 0, 0, null, new byte[0]));
                return;
            }
            if (StatisticsWire.Equal(knownHash, digest))
            {
                sender.SendPackage(NetPackageManager.GetPackage<NetPackageStatisticsReply>().Setup(id, 1, 0, 0, 0, digest, new byte[0]));
                return;
            }
            int count = (payload.Length + StatisticsWire.ChunkBytes - 1) / StatisticsWire.ChunkBytes;
            for (int i = 0; i < count; i++)
            {
                byte[] chunk = new byte[Math.Min(StatisticsWire.ChunkBytes, payload.Length - i * StatisticsWire.ChunkBytes)];
                Buffer.BlockCopy(payload, i * StatisticsWire.ChunkBytes, chunk, 0, chunk.Length);
                sender.SendPackage(NetPackageManager.GetPackage<NetPackageStatisticsReply>().Setup(id, 0, i, count, payload.Length, digest, chunk));
            }
        }

        internal static void HandleReply(int id, byte result, int index, int count, int total, byte[] hash, byte[] chunk)
        {
            Tick();
            if (IsServer || activeWorld == null || id != pendingRequest || pendingRequest == 0) return;
            try
            {
                if (result == 2) { pendingRequest = 0; Transfer.Reset(); error = "Die Statistikdatei auf dem Server fehlt oder ist ungueltig."; return; }
                if (result == 1)
                {
                    if (data == null || !StatisticsWire.Equal(digest, hash)) throw new InvalidDataException();
                    pendingRequest = 0; Transfer.Reset(); error = null; return;
                }
                byte[] completed = Transfer.Accept(id, index, count, total, hash, chunk);
                if (completed == null) return;
                var parsed = StatisticsWire.Decode(completed);
                data = parsed; digest = StatisticsWire.Hash(completed); error = null; pendingRequest = 0;
            }
            catch (Exception)
            {
                pendingRequest = 0; Transfer.Reset();
                error = "Ungueltige Statistikdaten empfangen; letzte gueltige Werte bleiben erhalten.";
            }
        }
    }
}
