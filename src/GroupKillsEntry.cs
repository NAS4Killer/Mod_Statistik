using System;
using System.Globalization;
using ModStatistik;
using UnityEngine;
public sealed class XUiC_StatistikPartyEntry : XUiC_PartyEntry
{
    private XUiV_Label killsLabel;
    private XUiV_Sprite star;
    private float nextRefresh;
    public override void Init() {
        base.Init();
        killsLabel=GetChildById("bloodmoonGroupKills").ViewComponent as XUiV_Label;
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
        if(star.IsVisible && killsLabel.UiTransform!=null) {
            var rendered=killsLabel.UiTransform.GetComponent<UILabel>();
            if(rendered!=null)star.Position=new Vector2i(360+(int)Math.Ceiling(Math.Min(120f,rendered.printedSize.x))+8,-6);
        }
    }
}
