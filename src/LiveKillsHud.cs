using System;
using System.Globalization;
using ModStatistik;
using UnityEngine;
public sealed class XUiC_BloodmoonKills : XUiController
{
    private XUiV_Label label, accuracyLabel, headshotsLabel;
    private XUiV_Sprite killsIcon, accuracyIcon, headshotsIcon;
    private float nextRefresh;
    public override void Init() {
        base.Init(); label = GetChildById("liveKills").ViewComponent as XUiV_Label;
        var accuracyChild=GetChildById("liveAccuracy");
        accuracyLabel=accuracyChild==null ? null : accuracyChild.ViewComponent as XUiV_Label;
        headshotsLabel = BloodmoonHudLayout.Label(this, "liveHeadshots");
        killsIcon = BloodmoonHudLayout.Icon(this, "liveKillsIcon");
        accuracyIcon = BloodmoonHudLayout.Icon(this, "liveAccuracyIcon");
        headshotsIcon = BloodmoonHudLayout.Icon(this, "liveHeadshotsIcon");
    }
    public override void Update(float dt)
    {
        base.Update(dt);
        if (label == null || Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.2f;
        var player = xui.playerUI.entityPlayer;
        long kills;
        kills = 0;
        bool visible = player != null && StatisticsSync.TryHudValue(player.EntityName, out kills);
        label.IsVisible = visible;
        BloodmoonHudLayout.Show(killsIcon, visible);
        BloodmoonHudLayout.Show(accuracyIcon, visible);
        BloodmoonHudLayout.Show(headshotsIcon, visible);
        if (headshotsLabel != null) headshotsLabel.IsVisible = visible;
        if (visible) label.Text = kills.ToString(CultureInfo.InvariantCulture);
        if (accuracyLabel == null) return;
        accuracyLabel.IsVisible = visible;
        accuracyLabel.Color = Color.white;
        if (visible)
        {
            ShotAccuracy accuracy;
            StatisticsSync.TryHudAccuracy(player.EntityName, out accuracy);
            accuracyLabel.Text = accuracy == null ? "\u2014" : accuracy.Percentage;
            if (BloodmoonAccuracyRanking.Leads(accuracy, GroupAccuracyRanking.Maximum(player))) accuracyLabel.Color = Color.green;
            int nextX = 40 + BloodmoonHudLayout.Width(label, 180) + 12;
            if (accuracyIcon != null) accuracyIcon.Position = new Vector2i(nextX, -8);
            accuracyLabel.Position = new Vector2i(nextX + 40, 0);
            nextX += 40 + BloodmoonHudLayout.Width(accuracyLabel, 180) + 12;
            if (headshotsIcon != null) headshotsIcon.Position = new Vector2i(nextX, -8);
            if (headshotsLabel != null)
            {
                headshotsLabel.Text = accuracy == null ? "\u2014" : accuracy.HeadshotsText;
                headshotsLabel.Position = new Vector2i(nextX + 40, 0);
            }
        }
    }
}

internal static class BloodmoonHudLayout
{
    internal static XUiV_Label Label(XUiController owner, string id)
    { var child = owner.GetChildById(id); return child == null ? null : child.ViewComponent as XUiV_Label; }
    internal static XUiV_Sprite Icon(XUiController owner, string id)
    { var child = owner.GetChildById(id); return child == null ? null : child.ViewComponent as XUiV_Sprite; }
    internal static void Show(XUiV_Sprite icon, bool visible) { if (icon != null) icon.IsVisible = visible; }
    internal static int Width(XUiV_Label label, int maximum)
    {
        var rendered = label.UiTransform == null ? null : label.UiTransform.GetComponent<UILabel>();
        return rendered != null && rendered.printedSize.x > 0f ?
            (int)Math.Ceiling(Math.Min(maximum, rendered.printedSize.x)) : maximum;
    }
}
