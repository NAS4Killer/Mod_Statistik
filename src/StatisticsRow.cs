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
            string text = "", accuracy = "", headshots = ""; bool red = false;
            bool visible = player != null && offset+i < data.Nights.Count;
            if (visible)
            {
                var night = data.Nights[offset+i]; long kills;
                text = night.Kills.TryGetValue(player, out kills) ? kills.ToString() : "—";
                red = night.Kills.ContainsKey(player) && kills == night.Kills.Values.Max();
                ShotAccuracy shots;
                accuracy = night.Accuracy.TryGetValue(player, out shots) ? shots.Percentage : "\u2014";
                headshots = shots == null ? "\u2014" : shots.HeadshotsText;
            }
            SetLabel("value"+i, text, red);
            SetLabel("accuracy"+i, accuracy, false);
            SetLabel("headshots"+i, headshots, false);
            SetIcon("killsIcon"+i, visible);
            SetIcon("accuracyIcon"+i, visible);
            SetIcon("headshotsIcon"+i, visible);
        }
    }
    private void SetLabel(string id, string text, bool red)
    {
        var child = GetChildById(id);
        var view = child == null ? null : child.ViewComponent as XUiV_Label;
        if(view != null) { view.Text=text; view.Color=red ? Color.red : Color.white; }
    }
    private void SetIcon(string id, bool visible)
    {
        var child = GetChildById(id);
        var view = child == null ? null : child.ViewComponent as XUiV_Sprite;
        if (view != null) view.IsVisible = visible;
    }
}
