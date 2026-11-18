using System;
using System.Collections.Generic;
using System.Linq;
using ModStatistik;
namespace UnityEngine { public struct Color {public int Value; public static Color red=new Color{Value=1}; public static Color white=new Color{Value=0};} public struct Color32 {public Color32(byte a,byte b,byte c,byte d){} public static explicit operator Color(Color32 c){return Color.white;}} }
public class XUiV_Label {public string Text; public UnityEngine.Color Color;}
public class XUiV_Sprite {public UnityEngine.Color Color; public bool IsVisible;}
public class XUiController {public object ViewComponent; private Dictionary<string,XUiController> labels=new Dictionary<string,XUiController>();public XUiController GetChildById(string id){if(!labels.ContainsKey(id))labels[id]=new XUiController{ViewComponent=(id=="background" || id.Contains("Icon"))?(object)new XUiV_Sprite():new XUiV_Label()};return labels[id];}}
internal static class RowTest
{
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    private static XUiV_Label Label(XUiC_StatistikRow row, string id) { return (XUiV_Label)row.GetChildById(id).ViewComponent; }
    private static int Main()
    {
        try
        {
            var historical = BloodmoonLog.Parse("=== Blutmond Tag 14 END test ===\nA start=0 end=10 bm_kills=10\nB start=0 end=10 bm_kills=10\n=== Blutmond Tag 7 END test ===\nA start=0 end=20 bm_kills=20\nB start=0 end=2 bm_kills=2\nC start=0 end=0 bm_kills=0\n");
            var data = BloodmoonLog.FromNights(historical.Nights.Select(n => n.Day == 14 ?
                new BloodmoonNight(n.Day, n.Kills.ToDictionary(p => p.Key, p => p.Value), new Dictionary<string, ShotAccuracy>
                { { "A", new ShotAccuracy(40, 30, 12) }, { "B", new ShotAccuracy(0, 0) } }) : n));
            var row = new XUiC_StatistikRow();
            row.Render(data, "A", 0, 20, 0);
            Check(Label(row, "record").Text == "20" && Label(row, "record").Color.Value == 1 && Label(row, "value0").Color.Value == 1, "Kill records and ties preserved.");
            Check(Label(row, "accuracy0").Text == "75 %" && Label(row, "accuracy0").Color.Value == 0, "Accuracy displayed separately from red kills.");
            Check(Label(row, "accuracy1").Text == "\u2014", "Historical accuracy remains unknown.");
            Check(Label(row, "headshots0").Text == "12" && Label(row, "headshots1").Text == "\u2014", "Known headshots and historical unknown kept distinct.");
            Check(((XUiV_Sprite)row.GetChildById("headshotsIcon0").ViewComponent).IsVisible, "headshot icon visible");
            row.Render(data, "B", 0, 20, 1);
            Check(Label(row, "record").Text == "10" && Label(row, "record").Color.Value == 0 && Label(row, "value0").Color.Value == 1 && Label(row, "value1").Color.Value == 0, "Other player's kill colors preserved.");
            Check(Label(row, "accuracy0").Text == "\u2014", "No-shot player has no percentage.");
            row.Render(data, "C", 0, 20, 2);
            Check(Label(row, "value0").Text == "\u2014" && Label(row, "value1").Text == "0", "Missing kills and zero kills remain distinct.");
            Check(Label(row, "accuracy0").Text == "\u2014" && Label(row, "accuracy1").Text == "\u2014", "Missing participant/history accuracy.");
            row.Render(data, "A", 1, 20, 0);
            Check(Label(row, "record").Text == "20" && Label(row, "accuracy0").Text == "\u2014" && Label(row, "accuracy1").Text == "", "Paging clears stale accuracy.");
            row.Render(data, null, 0, 20, 3);
            Check(Label(row, "record").Text == "" && Label(row, "value0").Color.Value == 0 && Label(row, "accuracy0").Text == "", "Unused row clears values.");
            Check(Label(row, "headshots0").Text == "" && !((XUiV_Sprite)row.GetChildById("headshotsIcon0").ViewComponent).IsVisible, "recycled empty rows hide headshots and icons");
            Console.WriteLine("PASS: kill records, ties, colors, missing values, zero shots, percentage labels and page changes.");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
