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
        bool visible = player != null && BloodmoonHudDisplay.TryValue(BloodmoonRecording.LiveNight,
            BloodmoonRecording.FinishedNight, BloodmoonRecording.FinishedAt, Time.unscaledTime, player.EntityName, out kills);
        label.IsVisible = visible;
        if (visible) label.Text = BloodmoonHudDisplay.Value(BloodmoonRecording.LiveNight,
            BloodmoonRecording.FinishedNight, player.EntityName).ToString(CultureInfo.InvariantCulture);
    }
}
