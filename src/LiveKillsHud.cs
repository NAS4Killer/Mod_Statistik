using System.Globalization;
using ModStatistik;
using UnityEngine;
public sealed class XUiC_BloodmoonKills : XUiController
{
    private XUiV_Label label;
    private float nextRefresh;
    public override void Init() { base.Init(); label = GetChildById("liveKills").ViewComponent as XUiV_Label; }
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
    }
}
