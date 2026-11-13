using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ModStatistik
{
    // Game-facing adapter. Shots only increment two counters; storage runs at most
    // once per five seconds, outside weapon execution.
    internal static class BloodmoonAccuracyRuntime
    {
        private static string directory, session;
        private static World world;
        private static bool server;
        private static BloodmoonAccuracyStore store;
        private static AccuracyReport local;
        private static float nextSample, nextSave, retryAt, finishedAt;
        private static int previousDay, finishedDay;
        internal static string Error { get; private set; }
        internal static long Revision { get { return store == null ? 0 : store.Revision; } }
        internal static long HistoryRevision { get; private set; }
        internal static bool IsRecording { get { return world != null && ActiveDay > 0; } }
        internal static bool NeedsFinalReport { get { return local != null && finishedDay == local.Day && Time.unscaledTime - finishedAt < 30f; } }

        internal static void Initialize(string path)
        {
            directory = Path.Combine(path, "Data");
            ModEvents.WorldShuttingDown.RegisterHandler(OnShutdown);
            ModEvents.PlayerDisconnected.RegisterHandler(OnDisconnected);
        }

        internal static void SetWorld(World value, bool isServer)
        {
            Flush();
            world = value; server = isServer; store = null; local = null;
            session = Guid.NewGuid().ToString("N");
            nextSample = nextSave = retryAt = finishedAt = 0;
            previousDay = finishedDay = 0; HistoryRevision = 0; Error = null;
        }

        internal static int DayAt(ulong now, int day, int dusk, int dawn)
        {
            if (day <= 0 || dusk < 0 || dusk > 23 || dawn < 0 || dawn > 23) return 0;
            ulong start = (ulong)(day - 1) * 24000UL + (ulong)dusk * 1000UL;
            ulong end = (ulong)day * 24000UL + (ulong)dawn * 1000UL;
            return now >= start && now < end ? day : 0;
        }

        private static int ActiveDay
        {
            get
            {
                if (world == null) return 0;
                var hours = GameUtils.CalcDuskDawnHours(GameStats.GetInt(EnumGameStats.DayLightLength));
                ulong now = world.GetWorldTime();
                if (server)
                {
                    var live = BloodmoonRecording.LiveNight;
                    return live == null ? 0 : DayAt(now, live.Day, hours.Item1, hours.Item2);
                }
                int remembered = local != null ? local.Day : previousDay;
                int day = DayAt(now, remembered, hours.Item1, hours.Item2);
                return day != 0 ? day : DayAt(now, GameStats.GetInt(EnumGameStats.BloodMoonDay), hours.Item1, hours.Item2);
            }
        }

        private static void EnsureLocal(int day)
        {
            if (day > 0 && (local == null || local.Day != day))
                local = new AccuracyReport { Day = day, Session = session, Shots = 0, Hits = 0 };
        }

        internal static void RecordShot(bool hit)
        {
            int day = ActiveDay;
            if (day == 0) return;
            EnsureLocal(day);
            if (local.Shots >= 1000000000L) return;
            local.Shots++;
            if (hit) local.Hits++;
        }

        internal static AccuracyReport Report(bool final = false)
        {
            if (local == null) return null;
            return new AccuracyReport { Day = local.Day, Session = local.Session, Shots = local.Shots, Hits = local.Hits, Final = final };
        }

        private static bool UpdatePhase(int day)
        {
            bool ended = previousDay > 0 && previousDay != day;
            if (ended) { finishedDay = previousDay; finishedAt = Time.unscaledTime; HistoryRevision++; }
            previousDay = day;
            return ended;
        }

        internal static void Tick()
        {
            if (world == null || Time.unscaledTime < retryAt) return;
            try
            {
                int day = ActiveDay;
                bool ended = UpdatePhase(day);
                if (ended && server && store != null)
                { ApplyLocal(); store.Flush(); }
                if (!GameManager.IsDedicatedServer) EnsureLocal(day);
                if (!server) return;
                if (store == null)
                {
                    string save = GameIO.GetSaveGameDir();
                    if (string.IsNullOrEmpty(save)) return;
                    string key = BitConverter.ToString(StatisticsWire.Hash(Encoding.UTF8.GetBytes(Path.GetFullPath(save))))
                        .Replace("-", "").ToLowerInvariant().Substring(0, 24);
                    store = new BloodmoonAccuracyStore(directory, key);
                    HistoryRevision++;
                }
                if (ended || Time.unscaledTime >= nextSample)
                {
                    nextSample = Time.unscaledTime + 1f;
                    ApplyLocal();
                }
                if (ended || Time.unscaledTime >= nextSave)
                { store.Flush(); nextSave = Time.unscaledTime + 5f; }
                Error = null;
            }
            catch (Exception e) { Failed(e); }
        }

        internal static Dictionary<string, ShotAccuracy> ForDay(int day)
        { return store == null ? new Dictionary<string, ShotAccuracy>(StringComparer.Ordinal) : store.ForDay(day); }

        internal static void Accept(int entityId, AccuracyReport report)
        {
            if (!server || world == null || store == null || report == null ||
                (report.Day != ActiveDay && !(report.Day == finishedDay && Time.unscaledTime - finishedAt <= 30f && AfterNight(report.Day)))) return;
            var player = world.Players.list.FirstOrDefault(p => p != null && p.entityId == entityId);
            var persistent = GameManager.Instance.GetPersistentPlayerList();
            var identity = persistent == null ? null : persistent.GetPlayerDataFromEntityID(entityId);
            if (player == null || identity == null || identity.PrimaryId == null) return;
            string id = identity.PrimaryId.ToString();
            string name = BloodmoonRecording.PlayerName(id, report.Day) ?? player.EntityName;
            if (store.Apply(id, name, report) && report.Day != ActiveDay) HistoryRevision++;
        }

        private static void ApplyLocal()
        {
            if (GameManager.IsDedicatedServer || local == null || world == null) return;
            var player = world.GetPrimaryPlayer();
            if (player != null) Accept(player.entityId, local);
        }

        private static bool AfterNight(int day)
        {
            var hours = GameUtils.CalcDuskDawnHours(GameStats.GetInt(EnumGameStats.DayLightLength));
            return world.GetWorldTime() >= (ulong)day * 24000UL + (ulong)hours.Item2 * 1000UL;
        }

        internal static void Flush()
        {
            if (!server || store == null) return;
            try { UpdatePhase(ActiveDay); ApplyLocal(); store.Flush(); }
            catch (Exception e) { Failed(e); }
        }

        private static void Failed(Exception e)
        {
            Error = "Trefferstatistik konnte nicht gespeichert werden; Data/bloodmoon_accuracy pruefen.";
            retryAt = Time.unscaledTime + 5f;
            Debug.LogWarning("[Mod_Statistik] Trefferstatistik: " + e.GetType().Name);
        }
        private static void OnShutdown(ref ModEvents.SWorldShuttingDownData args)
        { if (server) Flush(); else StatisticsSync.FlushAccuracy(); }
        private static void OnDisconnected(ref ModEvents.SPlayerDisconnectedData args) { Flush(); }
    }
}
