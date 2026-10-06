using System;
using System.IO;
using UnityEngine;

namespace ModStatistik
{
    public sealed class Api : IModApi
    {
        public void InitMod(Mod mod)
        {
            StatisticsRuntime.Initialize(mod.Path);
            if (!GameManager.IsDedicatedServer) StatisticsUpdater.Configure(mod.Path);
            Debug.Log("[Mod_Statistik] Version " + StatisticsRuntime.Version + " geladen.");
        }
    }

    internal static class StatisticsRuntime
    {
        internal const string Version = "0.0.1.0";
        internal static void Initialize(string modPath)
        {
            StatisticsSync.Initialize(modPath);
        }
    }

}
