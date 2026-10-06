using System.Linq;
using ModStatistik;
using UnityEngine;
public sealed class XUiC_StatistikRow : XUiController
{
    internal void Render(BloodmoonLog data, string player, int offset, long recordMaximum, int index)
    {
        var background = GetChildById("background").ViewComponent as XUiV_Sprite;
        if (background != null) background.Color = index % 2 == 0 ? (Color)new Color32(64,64,64,255) : (Color)new Color32(32,32,32,255);
        SetLabel("playerName", player ?? "", false);
        long record = player == null ? 0 : data.Nights.Where(n => n.Kills.ContainsKey(player)).Max(n => n.Kills[player]);
        SetLabel("record", player == null ? "" : record.ToString(), player != null && record == recordMaximum);
        for (int i=0; i<11; i++)
        {
            string text = ""; bool red = false;
            if (player != null && offset+i < data.Nights.Count)
            {
                var night = data.Nights[offset+i]; long kills;
                text = night.Kills.TryGetValue(player, out kills) ? kills.ToString() : "—";
                red = night.Kills.ContainsKey(player) && kills == night.Kills.Values.Max();
            }
            SetLabel("value"+i, text, red);
        }
    }
    private void SetLabel(string id, string text, bool red)
    {
        var view = GetChildById(id).ViewComponent as XUiV_Label;
        if(view != null) { view.Text=text; view.Color=red ? Color.red : Color.white; }
    }
}
