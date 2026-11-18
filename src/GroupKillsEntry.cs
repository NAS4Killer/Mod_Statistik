using System;
using System.Globalization;
using ModStatistik;
using UnityEngine;
public sealed class XUiC_StatistikPartyEntry : XUiC_PartyEntry
{
    private XUiV_Label killsLabel, accuracyLabel, headshotsLabel;
    private XUiV_Sprite star, killsIcon, accuracyIcon, headshotsIcon;
    private float nextRefresh;
    public override void Init() {
        base.Init();
        killsLabel=GetChildById("bloodmoonGroupKills").ViewComponent as XUiV_Label;
        var accuracyChild=GetChildById("bloodmoonGroupAccuracy");
        accuracyLabel=accuracyChild==null ? null : accuracyChild.ViewComponent as XUiV_Label;
        star=GetChildById("bloodmoonGroupStar").ViewComponent as XUiV_Sprite;
        headshotsLabel = BloodmoonHudLayout.Label(this, "bloodmoonGroupHeadshots");
        killsIcon = BloodmoonHudLayout.Icon(this, "bloodmoonGroupKillsIcon");
        accuracyIcon = BloodmoonHudLayout.Icon(this, "bloodmoonGroupAccuracyIcon");
        headshotsIcon = BloodmoonHudLayout.Icon(this, "bloodmoonGroupHeadshotsIcon");
    }
    public override void Update(float dt) {
        base.Update(dt);
        if(killsLabel==null || star==null || Time.unscaledTime<nextRefresh)return;
        nextRefresh=Time.unscaledTime+0.2f;
        var local=xui.playerUI.entityPlayer;
        long value=0;
        bool visible=Player!=null && StatisticsSync.TryHudValue(Player.EntityName,out value);
        killsLabel.IsVisible=visible;
        BloodmoonHudLayout.Show(killsIcon, visible);
        BloodmoonHudLayout.Show(accuracyIcon, visible);
        BloodmoonHudLayout.Show(headshotsIcon, visible);
        if (headshotsLabel != null) headshotsLabel.IsVisible = visible;
        killsLabel.Color=(Player==local)?Color.red:Color.white;
        if(visible)killsLabel.Text=value.ToString(CultureInfo.InvariantCulture);
        star.IsVisible=visible && GroupKillRanking.Leads(value,GroupKillRanking.Maximum(local));
        int nextX=392+BloodmoonHudLayout.Width(killsLabel,120)+8;
        if(star.IsVisible) { star.Position=new Vector2i(nextX,-6); nextX+=34; }
        if(accuracyLabel!=null) {
            accuracyLabel.IsVisible=visible;
            accuracyLabel.Color=Color.white;
            if(visible) {
                ShotAccuracy accuracy;
                StatisticsSync.TryHudAccuracy(Player.EntityName,out accuracy);
                accuracyLabel.Text=accuracy==null ? "\u2014" : accuracy.Percentage;
                if(BloodmoonAccuracyRanking.Leads(accuracy,GroupAccuracyRanking.Maximum(local)))accuracyLabel.Color=Color.green;
                if (accuracyIcon != null) accuracyIcon.Position = new Vector2i(nextX,-6);
                accuracyLabel.Position=new Vector2i(nextX+32,-6);
                nextX += 32 + BloodmoonHudLayout.Width(accuracyLabel,110) + 8;
                if (headshotsIcon != null) headshotsIcon.Position = new Vector2i(nextX,-6);
                if (headshotsLabel != null) {
                    headshotsLabel.Text = accuracy == null ? "\u2014" : accuracy.HeadshotsText;
                    headshotsLabel.Position = new Vector2i(nextX+32,-6);
                }
            }
        }
    }
}
