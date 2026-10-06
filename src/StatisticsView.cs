using System;
using System.Linq;
using ModStatistik;
using UnityEngine;

public sealed class XUiC_BloodmoonStatistics : XUiController
{
    private XUiC_TextInput urlInput;
    private XUiC_StatistikRow[] rows;
    private XUiC_TabSelectorTab tab;
    private BloodmoonLog shownData;
    private int nightPage;
    private int playerPage;
    private bool wasSelected;
    private float nextRefresh;
    private float nextDownload;
    private string message;
    private XUiC_Paging playerPager, nightPager;

    public override void Init()
    {
        base.Init();
        urlInput = GetChildById("statisticsUrl") as XUiC_TextInput;
        if (urlInput != null) urlInput.SupportBbCode = false;
        rows = GetChildById("statisticsRows").Children.OfType<XUiC_StatistikRow>().ToArray();
        tab = Parent as XUiC_TabSelectorTab;
        Bind("saveStatisticsUrl", SaveUrl);
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
            if (urlInput != null) urlInput.Text = StatisticsRuntime.Download.Snapshot().Url;
            StatisticsRuntime.Download.RefreshAsync();
            Render();
        }
        if (selected && Time.unscaledTime >= nextDownload)
        { nextDownload = Time.unscaledTime + 60f; if (wasSelected) StatisticsRuntime.Download.RefreshAsync(); }
        wasSelected = selected;
        if (selected && Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + 0.2f;
            Render();
        }
    }

    private void SaveUrl()
    {
        try
        {
            StatisticsRuntime.Download.SaveUrl(urlInput == null ? "" : urlInput.Text);
            message = "Adresse gespeichert.";
            StatisticsRuntime.Download.RefreshAsync();
        }
        catch (ArgumentException e) { message = e.Message; }
        catch (InvalidOperationException e) { message = e.Message; }
        catch (Exception) { message = "Adresse konnte nicht gespeichert werden."; }
        Render();
    }

    private void Render()
    {
        var state = StatisticsRuntime.Download.Snapshot();
        if (state.Data != shownData) { nightPage = 0; playerPage = 0; shownData = state.Data; }
        int nightPages = Math.Max(1, state.Data == null ? 0 : (state.Data.Nights.Count + 10) / 11);
        int playerPages = Math.Max(1, state.Data == null ? 0 : (state.Data.Players.Count + 12) / 13);
        nightPage = Math.Min(nightPage, nightPages - 1);
        playerPage = Math.Min(playerPage, playerPages - 1);
        SetLabel("statisticsVersion", "Version " + StatisticsDownload.Version);
        var updateLabel = GetChildById("statisticsUpdate").GetChildById("btnLabel").ViewComponent as XUiV_Label;
        if (updateLabel != null) { updateLabel.Text = StatisticsUpdater.Caption; updateLabel.Color = StatisticsUpdater.IsGreen ? (Color)new Color32(70,230,70,255) : Color.white; }
        SetLabel("statisticsUpdateStatus", StatisticsUpdater.Status);
        var updateStatus = GetChildById("statisticsUpdateStatus").ViewComponent as XUiV_Label;
        if (updateStatus != null) updateStatus.Color = StatisticsUpdater.NeedsRestart ? Color.red : Color.white;
        GetChildById("statisticsReleaseNotes").ViewComponent.IsVisible = StatisticsUpdater.ReleaseNotesUrl != null;
        string active = state.Url.Length == 0 ? "Data/bloodmoon_kills.txt" : state.Url;
        string edited = urlInput == null ? "" : urlInput.Text;
        SetLabel("statisticsStatus", state.LastError ?? (edited != state.Url ? "Aktuell: " + active : message ?? (state.Url.Length == 0 ? active : "")));
        playerPager.SetLastPageByElementsAndPageLength(state.Data == null ? 0 : state.Data.Players.Count, 13);
        nightPager.SetLastPageByElementsAndPageLength(state.Data == null ? 0 : state.Data.Nights.Count, 11);
        playerPager.CurrentPageNumber = playerPage;
        nightPager.CurrentPageNumber = nightPage;
        GetChildById("nightPager").ViewComponent.IsVisible = nightPages > 1;
        for (int i=0; i<11; i++)
            SetLabel("nightHeader"+i, state.Data != null && nightPage*11+i < state.Data.Nights.Count ? "Tag " + state.Data.Nights[nightPage*11+i].Day : "");
        long maximum = state.Data == null || state.Data.Players.Count == 0 ? 0 : state.Data.Nights.SelectMany(n => n.Kills.Values).Max();
        for (int i=0; i<rows.Length; i++)
        {
            int index = playerPage*13+i;
            rows[i].Render(state.Data, state.Data != null && index < state.Data.Players.Count ? state.Data.Players[index] : null, nightPage*11, maximum, i);
        }
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
