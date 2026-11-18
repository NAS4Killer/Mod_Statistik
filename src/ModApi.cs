using System;
using System.IO;
using UnityEngine;

namespace ModStatistik
{
    public sealed class Api : IModApi
    {
        public void InitMod(Mod mod)
        {
            StatisticsTransport.Initialize();
            StatisticsRuntime.Initialize(mod.Path);
            if (!GameManager.IsDedicatedServer) StatisticsUpdater.Configure(mod.Path);
            Debug.Log("[Mod_Statistik] Version " + StatisticsRuntime.Version + " geladen.");
        }
    }

    internal static class StatisticsRuntime
    {
        internal static readonly string Version = typeof(StatisticsRuntime).Assembly.GetName().Version.ToString();
        internal static void Initialize(string modPath)
        {
            StatisticsSync.Initialize(modPath);
        }
    }

}
