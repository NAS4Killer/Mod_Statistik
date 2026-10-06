using System;
using System.IO;
using UnityEngine;

namespace ModStatistik
{
    public sealed class Api : IModApi
    {
        public void InitMod(Mod mod)
        {
            if (GameManager.IsDedicatedServer) return;
            StatisticsRuntime.Initialize(mod.Path);
            StatisticsUpdater.Configure(mod.Path);
            var handler = new GameObject("Mod_Statistik_Hotkey");
            UnityEngine.Object.DontDestroyOnLoad(handler);
            handler.AddComponent<StatisticsHotkey>();
            Debug.Log("[Mod_Statistik] Version " + StatisticsDownload.Version + " geladen.");
        }
    }

    internal static class StatisticsRuntime
    {
        internal const string TabKey = "modStatistikBlutmond";
        internal static StatisticsDownload Download;
        internal static void Initialize(string modPath)
        {
            string preferences = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "7DaysToDie", "Mod_Statistik");
            Download = new StatisticsDownload(preferences, Path.Combine(modPath, "Data", "bloodmoon_kills.txt"));
        }
    }

    public sealed class StatisticsHotkey : MonoBehaviour
    {
        private readonly DoublePress input = new DoublePress();
        private int selectionFrame = -1;
        private XUi requestedUi;

        private void LateUpdate()
        {
            var playerUi = LocalPlayerUI.GetUIForPrimaryPlayer();
            XUi ui = playerUi == null ? null : playerUi.xui;
            bool hasPlayer = GameManager.Instance != null && GameManager.Instance.World != null && GameManager.Instance.World.GetPrimaryPlayer() != null;
            if (!hasPlayer || ui == null || !ui.IsReady || UIInput.selection != null || playerUi.windowManager.IsWindowOpen("chat") || playerUi.windowManager.IsWindowOpen("ingameMenu"))
            {
                input.Reset();
                selectionFrame = -1;
                requestedUi = null;
                return;
            }

            if (input.Update(Time.unscaledTime, Input.GetKeyDown(KeyCode.I)))
            {
                // LateUpdate runs after the game's normal single-I handling.
                playerUi.windowManager.CloseAllOpenModalWindows("players");
                playerUi.windowManager.Open("players", true);
                requestedUi = ui;
                selectionFrame = Time.frameCount + 1;
            }

            if (selectionFrame >= 0 && Time.frameCount >= selectionFrame)
            {
                selectionFrame = -1;
                if (requestedUi != ui || !playerUi.windowManager.IsWindowOpen("players")) return;
                var group = ui.GetWindowGroupById("players");
                var tabs = group == null ? null : group.Controller.GetChildById("tabs") as XUiC_TabSelector;
                if (tabs != null) tabs.SelectTabByName(StatisticsRuntime.TabKey, true);
                else Debug.LogWarning("[Mod_Statistik] Spieler-TabSelector nicht gefunden.");
                requestedUi = null;
            }
        }
    }
}
