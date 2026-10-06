using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace ModStatistik
{
    internal static class BloodmoonRecording
    {
        private static string directory;
        private static World world;
        private static BloodmoonRecorder recorder;
        private static float retryAt, nextSample;
        internal static BloodmoonNight LiveNight { get { return recorder == null ? null : recorder.LiveNight; } }
        internal static BloodmoonNight FinishedNight { get { return recorder == null ? null : recorder.FinishedNight; } }
        internal static float FinishedAt { get; private set; }
        private static ulong lastTime = ulong.MaxValue;
        internal static string Error { get; private set; }

        internal static void Initialize(string path)
        {
            directory = Path.Combine(path, "Data");
            ModEvents.PlayerDisconnected.RegisterHandler(OnDisconnected);
            ModEvents.WorldShuttingDown.RegisterHandler(OnShutdown);
        }
        internal static void SetWorld(World value, bool localOrServer)
        {
            if (recorder != null)
                try { recorder.Flush(); } catch (Exception e) { Debug.LogWarning("[Mod_Statistik] Zwischenstand: " + e.GetType().Name); }
            world = localOrServer ? value : null;
            recorder = null; retryAt = 0; nextSample = 0; FinishedAt = 0; lastTime = ulong.MaxValue; Error = null;
        }
        internal static void Tick()
        {
            if (world == null || Time.unscaledTime < retryAt) return;
            try
            {
                if (recorder == null)
                {
                    string save = GameIO.GetSaveGameDir();
                    if (string.IsNullOrEmpty(save)) return;
                    string key = BitConverter.ToString(StatisticsWire.Hash(Encoding.UTF8.GetBytes(Path.GetFullPath(save))))
                        .Replace("-", "").ToLowerInvariant().Substring(0, 24);
                    recorder = new BloodmoonRecorder(directory, key);
                }
                ulong now = world.GetWorldTime();
                if (now == lastTime) return;
                var hours = GameUtils.CalcDuskDawnHours(GameStats.GetInt(EnumGameStats.DayLightLength));
                bool inside = recorder.Active;
                if (inside && Time.unscaledTime < nextSample) return;
                var previousFinished = recorder.FinishedNight;
                recorder.Step(now, GameStats.GetInt(EnumGameStats.BloodMoonDay), hours.Item1, hours.Item2,
                    Time.unscaledTime, Samples);
                if (!ReferenceEquals(previousFinished, recorder.FinishedNight)) FinishedAt = Time.unscaledTime;
                nextSample = Time.unscaledTime + 1f;
                lastTime = now; Error = null;
            }
            catch (Exception e)
            {
                retryAt = Time.unscaledTime + 5;
                Error = "Blutmond-Aufzeichnung fehlgeschlagen; Schreibrechte und Dateien in Data pruefen.";
                Debug.LogWarning("[Mod_Statistik] Aufzeichnung: " + e.GetType().Name);
            }
        }
        private static IEnumerable<KillSample> Samples()
        {
            var list = new List<KillSample>();
            var persistent = GameManager.Instance.GetPersistentPlayerList();
            foreach (var player in world.Players.list)
            {
                if (player == null) continue;
                var identity = persistent == null ? null : persistent.GetPlayerDataFromEntityID(player.entityId);
                if (identity == null || identity.PrimaryId == null) continue;
                list.Add(new KillSample(identity.PrimaryId.ToString(), player.EntityName, player.KilledZombies));
            }
            return list;
        }
        private static void OnDisconnected(ref ModEvents.SPlayerDisconnectedData args) { SaveRemaining(); }
        private static void OnShutdown(ref ModEvents.SWorldShuttingDownData args) { SaveRemaining(); }
        private static void SaveRemaining()
        {
            if (world == null || recorder == null) return;
            try { if (recorder.Active) recorder.Capture(Samples()); recorder.Flush(); }
            catch (Exception e) { Debug.LogWarning("[Mod_Statistik] Zwischenstand: " + e.GetType().Name); }
        }
    }
}
