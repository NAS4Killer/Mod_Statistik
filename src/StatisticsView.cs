using System;
using System.Linq;
using ModStatistik;
using UnityEngine;

public sealed class XUiC_BloodmoonStatistics : XUiController
{
    private XUiC_StatistikRow[] rows;
    private XUiC_TabSelectorTab tab;
    private BloodmoonLog shownData;
    private int nightPage;
    private int playerPage;
    private bool wasSelected;
    private float nextRefresh;
    private float nextSync;
    private string message;
    private XUiC_Paging playerPager, nightPager;

    public override void Init()
    {
        base.Init();
        rows = GetChildById("statisticsRows").Children.OfType<XUiC_StatistikRow>().ToArray();
        tab = Parent as XUiC_TabSelectorTab;
        Bind("statisticsUpdate", delegate { if (StatisticsUpdater.Press()) Application.OpenURL(StatisticsUpdater.RepositoryUrl); });
        Bind("statisticsReleaseNotes", delegate { string url = StatisticsUpdater.ReleaseNotesUrl; if (url != null) Application.OpenURL(url); });
        playerPager = GetChildById("playerPager") as XUiC_Paging;
        nightPager = GetChildById("nightPager") as XUiC_Paging;
        playerPager.OnPageChanged += delegate { playerPage = playerPager.GetPage(); Render(); };
        nightPager.OnPageChanged += delegate { nightPage = nightPager.GetPage(); Render(); };
    }

    public override void OnOpen()
    {
        base.OnOpen();
        wasSelected = false;
    }

    public override void OnClose()
    {
        wasSelected = false;
        base.OnClose();
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        bool selected = tab != null && tab.TabSelected && xui.playerUI.windowManager.IsWindowOpen("players");
        if (selected && !wasSelected)
        {
            message = null;
            StatisticsUpdater.EnsureChecked();
            StatisticsSync.Request();
            Render();
        }
        if (selected && Time.unscaledTime >= nextSync)
        { nextSync = Time.unscaledTime + 5f; if (wasSelected) StatisticsSync.Request(); }
        wasSelected = selected;
        if (selected && Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + 1f;
            Render();
        }
    }

    private void Render()
    {
        var state = StatisticsSync.Snapshot();
        if (state.Data != shownData) shownData = state.Data;
        int nightPages = Math.Max(1, state.Data == null ? 0 : (state.Data.Nights.Count + 10) / 11);
        int playerPages = Math.Max(1, state.Data == null ? 0 : (state.Data.Players.Count + 11) / 12);
        nightPage = Math.Min(nightPage, nightPages - 1);
        playerPage = Math.Min(playerPage, playerPages - 1);
        SetLabel("statisticsVersion", "Version " + StatisticsRuntime.Version);
        var updateLabel = GetChildById("statisticsUpdate").GetChildById("btnLabel").ViewComponent as XUiV_Label;
        if (updateLabel != null) { updateLabel.Text = StatisticsUpdater.Caption; updateLabel.Color = StatisticsUpdater.IsGreen ? (Color)new Color32(70,230,70,255) : Color.white; }
        SetLabel("statisticsUpdateStatus", StatisticsUpdater.Status);
        UpdateBloodmoonCountdown();
        var updateStatus = GetChildById("statisticsUpdateStatus").ViewComponent as XUiV_Label;
        if (updateStatus != null) updateStatus.Color = StatisticsUpdater.NeedsRestart ? Color.red : Color.white;
        bool showReleaseNotes = StatisticsUpdater.ReleaseNotesUrl != null;
        GetChildById("statisticsReleaseNotes").ViewComponent.IsVisible = showReleaseNotes;
        GetChildById("statisticsUpdateStatus").ViewComponent.IsVisible = !showReleaseNotes && StatisticsUpdater.Status.Length != 0;
        SetLabel("statisticsStatus", state.Error ?? message ?? (state.Busy && state.Data == null ? "Statistik wird vom Server geladen …" : ""));
        playerPager.SetLastPageByElementsAndPageLength(state.Data == null ? 0 : state.Data.Players.Count, 12);
        nightPager.SetLastPageByElementsAndPageLength(state.Data == null ? 0 : state.Data.Nights.Count, 11);
        playerPager.CurrentPageNumber = playerPage;
        nightPager.CurrentPageNumber = nightPage;
        GetChildById("nightPager").ViewComponent.IsVisible = nightPages > 1;
        for (int i=0; i<11; i++)
            SetLabel("nightHeader"+i, state.Data != null && nightPage*11+i < state.Data.Nights.Count ? "Tag " + state.Data.Nights[nightPage*11+i].Day : "");
        long maximum = state.Data == null || state.Data.Players.Count == 0 ? 0 : state.Data.Nights.SelectMany(n => n.Kills.Values).Max();
        for (int i=0; i<rows.Length; i++)
        {
            int index = playerPage*12+i;
            rows[i].Render(state.Data, state.Data != null && index < state.Data.Players.Count ? state.Data.Players[index] : null, nightPage*11, maximum, i);
        }
    }

    private void UpdateBloodmoonCountdown()
    {
        var world = GameManager.Instance == null ? null : GameManager.Instance.World;
        if (world == null) { SetLabel("bloodmoonCountdown", "Nächster Blutmond: noch nicht bekannt"); return; }
        int day = GameStats.GetInt(EnumGameStats.BloodMoonDay);
        var hours = GameUtils.CalcDuskDawnHours(GameStats.GetInt(EnumGameStats.DayLightLength));
        int dayLength = GameStats.GetInt(EnumGameStats.DayNightLength);
        if (dayLength <= 0) dayLength = GamePrefs.GetInt(EnumGamePrefs.DayNightLength);
        double speed = BloodmoonCountdown.ResolveSpeed(0, dayLength);
        SetLabel("bloodmoonCountdown", BloodmoonCountdown.Format(world.GetWorldTime(), day, hours.Item1, hours.Item2, speed));
    }

    private void Bind(string id, Action action)
    {
        var button = GetChildById(id) as XUiC_SimpleButton;
        if (button != null) button.OnPressed += delegate(XUiController sender, int mouseButton)
        {
            try { action(); }
            catch (InvalidOperationException e) { message = e.Message; }
            catch (Exception) { message = "Aktion konnte nicht ausgeführt werden."; }
        };
    }

    private void SetLabel(string id, string text)
    {
        var view = GetChildById(id).ViewComponent as XUiV_Label;
        if (view != null) view.Text = text;
    }
}
