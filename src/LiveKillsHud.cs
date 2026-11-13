using System;
using System.Globalization;
using ModStatistik;
using UnityEngine;
public sealed class XUiC_BloodmoonKills : XUiController
{
    private XUiV_Label label, accuracyLabel;
    private float nextRefresh;
    public override void Init() {
        base.Init(); label = GetChildById("liveKills").ViewComponent as XUiV_Label;
        var accuracyChild=GetChildById("liveAccuracy");
        accuracyLabel=accuracyChild==null ? null : accuracyChild.ViewComponent as XUiV_Label;
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
            float printedWidth = 180f;
            if (label.UiTransform != null)
            {
                var rendered = label.UiTransform.GetComponent<UILabel>();
                if (rendered != null && rendered.printedSize.x > 0f) printedWidth = Math.Min(180f, rendered.printedSize.x);
            }
            accuracyLabel.Position = new Vector2i((int)Math.Ceiling(printedWidth) + 12, 0);
        }
    }
}
