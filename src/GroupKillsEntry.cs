using System;
using System.Globalization;
using ModStatistik;
using UnityEngine;
public sealed class XUiC_StatistikPartyEntry : XUiC_PartyEntry
{
    private XUiV_Label killsLabel, accuracyLabel;
    private XUiV_Sprite star;
    private float nextRefresh;
    public override void Init() {
        base.Init();
        killsLabel=GetChildById("bloodmoonGroupKills").ViewComponent as XUiV_Label;
        var accuracyChild=GetChildById("bloodmoonGroupAccuracy");
        accuracyLabel=accuracyChild==null ? null : accuracyChild.ViewComponent as XUiV_Label;
        star=GetChildById("bloodmoonGroupStar").ViewComponent as XUiV_Sprite;
    }
    public override void Update(float dt) {
        base.Update(dt);
        if(killsLabel==null || star==null || Time.unscaledTime<nextRefresh)return;
        nextRefresh=Time.unscaledTime+0.2f;
        var local=xui.playerUI.entityPlayer;
        long value=0;
        bool visible=Player!=null && StatisticsSync.TryHudValue(Player.EntityName,out value);
        killsLabel.IsVisible=visible;
        killsLabel.Color=(Player==local)?Color.red:Color.white;
        if(visible)killsLabel.Text=value.ToString(CultureInfo.InvariantCulture);
        star.IsVisible=visible && GroupKillRanking.Leads(value,GroupKillRanking.Maximum(local));
        float printedWidth=120f;
        if(killsLabel.UiTransform!=null) {
            var rendered=killsLabel.UiTransform.GetComponent<UILabel>();
            if(rendered!=null && rendered.printedSize.x>0f)printedWidth=Math.Min(120f,rendered.printedSize.x);
        }
        int nextX=360+(int)Math.Ceiling(printedWidth)+8;
        if(star.IsVisible) { star.Position=new Vector2i(nextX,-6); nextX+=34; }
        if(accuracyLabel!=null) {
            accuracyLabel.IsVisible=visible;
            accuracyLabel.Color=Color.white;
            if(visible) {
                ShotAccuracy accuracy;
                StatisticsSync.TryHudAccuracy(Player.EntityName,out accuracy);
                accuracyLabel.Text=accuracy==null ? "\u2014" : accuracy.Percentage;
                if(BloodmoonAccuracyRanking.Leads(accuracy,GroupAccuracyRanking.Maximum(local)))accuracyLabel.Color=Color.green;
                accuracyLabel.Position=new Vector2i(nextX,-6);
            }
        }
    }
}
