using System;
using System.Collections.Generic;
using ModStatistik;

namespace UnityEngine
{
    public struct Color
    {
        public int Value;
        public static Color white = new Color { Value = 0 };
        public static Color red = new Color { Value = 1 };
        public static Color green = new Color { Value = 2 };
    }
    public struct Vector2 { public float x, y; }
    public sealed class Transform
    {
        public UILabel Rendered = new UILabel();
        public T GetComponent<T>() where T : class { return Rendered as T; }
    }
    public static class Time { public static float unscaledTime; }
}
public struct Vector2i
{
    public int x, y;
    public Vector2i(int x, int y) { this.x = x; this.y = y; }
}
public sealed class UILabel { public UnityEngine.Vector2 printedSize; }
public class XUiV_Label
{
    public string Text;
    public bool IsVisible;
    public UnityEngine.Color Color;
    public Vector2i Position, Size;
    public int Width, Height;
    public UnityEngine.Transform UiTransform = new UnityEngine.Transform();
}
public class XUiV_Sprite
{
    public bool IsVisible;
    public UnityEngine.Color Color;
    public Vector2i Position, Size;
    public int Width, Height;
}
public sealed class Party { public List<EntityPlayer> MemberList = new List<EntityPlayer>(); }
public class EntityPlayer { public string EntityName; public Party Party; }
public sealed class PlayerUi { public EntityPlayer entityPlayer; }
public sealed class XUi { public PlayerUi playerUI = new PlayerUi(); }
public class XUiController
{
    public object ViewComponent;
    public XUi xui = new XUi();
    private readonly Dictionary<string, XUiController> children = new Dictionary<string, XUiController>();
    public virtual void Init() { }
    public virtual void Update(float dt) { }
    public XUiController GetChildById(string id)
    {
        XUiController child;
        if (children.TryGetValue(id, out child)) return child;
        child = new XUiController();
        if (id == "bloodmoonGroupStar" || id.EndsWith("Icon"))
            child.ViewComponent = new XUiV_Sprite { Position = new Vector2i(486, -6), Width = 26, Height = 26, Size = new Vector2i(26, 26), Color = UnityEngine.Color.red };
        else
        {
            bool party = id == "bloodmoonGroupKills" || id == "bloodmoonGroupAccuracy";
            var label = new XUiV_Label
            {
                Position = new Vector2i(party ? 360 : 0, party ? -6 : 0),
                Width = party ? 120 : 180,
                Height = party ? 30 : 48,
                Size = new Vector2i(party ? 120 : 180, party ? 30 : 48),
                Color = id == "liveKills" ? UnityEngine.Color.red : UnityEngine.Color.white
            };
            label.UiTransform.Rendered.printedSize = new UnityEngine.Vector2 { x = 38, y = 26 };
            child.ViewComponent = label;
        }
        children.Add(id, child);
        return child;
    }
}
public class XUiC_PartyEntry : XUiController { public EntityPlayer Player; }
namespace ModStatistik
{
    internal static class StatisticsSync
    {
        internal static readonly Dictionary<string, long> Kills = new Dictionary<string, long>();
        internal static readonly Dictionary<string, ShotAccuracy> Accuracy = new Dictionary<string, ShotAccuracy>();
        internal static bool TryHudValue(string player, out long value) { return Kills.TryGetValue(player, out value); }
        internal static bool TryHudAccuracy(string player, out ShotAccuracy value) { return Accuracy.TryGetValue(player, out value); }
    }
}
internal static class BloodmoonHudAccuracyUiTests
{
    private static void Need(bool value, string message) { if (!value) throw new Exception(message); }
    private static XUiV_Label Label(XUiController controller, string name) { return (XUiV_Label)controller.GetChildById(name).ViewComponent; }
    private static XUiV_Sprite Star(XUiController controller) { return (XUiV_Sprite)controller.GetChildById("bloodmoonGroupStar").ViewComponent; }
    private static void Refresh(params XUiController[] controllers)
    {
        UnityEngine.Time.unscaledTime += 1f;
        foreach (var controller in controllers) controller.Update(1f);
    }
    private static void Accuracy(XUiController controller, string name, string expected, bool green)
    {
        var label = Label(controller, name);
        Need(label.IsVisible, name + " should be visible");
        Need(label.Text == expected, name + " text: expected " + expected + ", got " + label.Text);
        Need(label.Color.Value == (green ? UnityEngine.Color.green.Value : UnityEngine.Color.white.Value), name + " leader color");
    }
    private static XUiC_StatistikPartyEntry Entry(EntityPlayer local, EntityPlayer player)
    {
        var entry = new XUiC_StatistikPartyEntry { Player = player };
        entry.xui.playerUI.entityPlayer = local;
        entry.Init();
        return entry;
    }
    private static int Main()
    {
        try
        {
            var a = new EntityPlayer { EntityName = "A" };
            var b = new EntityPlayer { EntityName = "B" };
            var c = new EntityPlayer { EntityName = "C" };
            var outside = new EntityPlayer { EntityName = "Outside" };
            a.Party = new Party();
            a.Party.MemberList.Add(a); a.Party.MemberList.Add(b); a.Party.MemberList.Add(c);
            StatisticsSync.Kills.Add("A", 12); StatisticsSync.Kills.Add("B", 8); StatisticsSync.Kills.Add("C", 3); StatisticsSync.Kills.Add("Outside", 1000);
            StatisticsSync.Accuracy.Add("A", new ShotAccuracy(4, 2, 1));
            StatisticsSync.Accuracy.Add("B", new ShotAccuracy(4, 3));
            StatisticsSync.Accuracy.Add("C", new ShotAccuracy(4, 1));
            StatisticsSync.Accuracy.Add("Outside", new ShotAccuracy(4, 4));
            var ownEntry = Entry(a, a); var otherEntry = Entry(a, b); var thirdEntry = Entry(a, c);
            var live = new XUiC_BloodmoonKills(); live.xui.playerUI.entityPlayer = a; live.Init();
            Refresh(ownEntry, otherEntry, thirdEntry, live);
            Accuracy(ownEntry, "bloodmoonGroupAccuracy", "50 %", false);
            Accuracy(otherEntry, "bloodmoonGroupAccuracy", "75 %", true);
            Accuracy(thirdEntry, "bloodmoonGroupAccuracy", "25 %", false);
            Accuracy(live, "liveAccuracy", "50 %", false);
            Need(Label(ownEntry, "bloodmoonGroupKills").Text == "12" && Label(ownEntry, "bloodmoonGroupKills").Color.Value == 1, "own party kills remain red");
            Need(Label(otherEntry, "bloodmoonGroupKills").Text == "8" && Label(otherEntry, "bloodmoonGroupKills").Color.Value == 0, "other party kills remain white");
            Need(Label(live, "liveKills").Text == "12" && Label(live, "liveKills").Color.Value == 1, "own standalone kills remain red");
            Need(Star(ownEntry).IsVisible && !Star(otherEntry).IsVisible && Star(ownEntry).Color.Value == 1, "red kill leader star preserved independently of accuracy winner");
            Need(GroupAccuracyRanking.Maximum(a) == 75m, "higher accuracy outside party does not affect party winner");
            Need(Label(ownEntry, "bloodmoonGroupHeadshots").Text == "1" && Label(live, "liveHeadshots").Text == "1", "headshots in both HUDs");
            Need(Label(otherEntry, "bloodmoonGroupHeadshots").Text == "\u2014", "old clients headshots unknown");
            Need(Label(live, "liveHeadshots").Position.x > Label(live, "liveAccuracy").Position.x + 38, "headshots after accuracy");
            Need(((XUiV_Sprite)live.GetChildById("liveHeadshotsIcon").ViewComponent).IsVisible, "headshot icon shown");
            int ownX = Label(ownEntry, "bloodmoonGroupAccuracy").Position.x;
            Need(ownX >= 360 + 38 + 8, "party percentage appears after rendered kills");
            Need(ownX >= Star(ownEntry).Position.x + 26, "party percentage does not overlap visible kill star");
            Need(Label(otherEntry, "bloodmoonGroupAccuracy").Position.x >= 360 + 38 + 8, "nonleader percentage appears after rendered kills");
            Need(Label(live, "liveAccuracy").Position.x >= 38 + 8, "standalone percentage appears after rendered kills");

            StatisticsSync.Accuracy["A"] = new ShotAccuracy(8, 6);
            Refresh(ownEntry, otherEntry, live);
            Accuracy(ownEntry, "bloodmoonGroupAccuracy", "75 %", true);
            Accuracy(otherEntry, "bloodmoonGroupAccuracy", "75 %", true);
            Accuracy(live, "liveAccuracy", "75 %", true);
            StatisticsSync.Accuracy["C"] = new ShotAccuracy(12, 9);
            Refresh(ownEntry, otherEntry, thirdEntry, live);
            Accuracy(thirdEntry, "bloodmoonGroupAccuracy", "75 %", true);

            StatisticsSync.Accuracy["A"] = new ShotAccuracy(3, 1);
            StatisticsSync.Accuracy["B"] = new ShotAccuracy(1000, 333);
            StatisticsSync.Accuracy["C"] = new ShotAccuracy(4, 1);
            Refresh(ownEntry, otherEntry, thirdEntry, live);
            Accuracy(ownEntry, "bloodmoonGroupAccuracy", "33,3 %", true);
            Accuracy(otherEntry, "bloodmoonGroupAccuracy", "33,3 %", true);
            Accuracy(thirdEntry, "bloodmoonGroupAccuracy", "25 %", false);
            StatisticsSync.Accuracy["A"] = new ShotAccuracy(4, 1);
            Refresh(ownEntry, otherEntry, live);
            Accuracy(ownEntry, "bloodmoonGroupAccuracy", "25 %", false);
            Accuracy(live, "liveAccuracy", "25 %", false);
            Accuracy(otherEntry, "bloodmoonGroupAccuracy", "33,3 %", true);

            StatisticsSync.Accuracy["A"] = new ShotAccuracy(0, 0);
            StatisticsSync.Accuracy.Remove("B");
            StatisticsSync.Accuracy["C"] = new ShotAccuracy(5, 0);
            Refresh(ownEntry, otherEntry, thirdEntry, live);
            Accuracy(ownEntry, "bloodmoonGroupAccuracy", "\u2014", false);
            Accuracy(otherEntry, "bloodmoonGroupAccuracy", "\u2014", false);
            Accuracy(thirdEntry, "bloodmoonGroupAccuracy", "0 %", true);
            Accuracy(live, "liveAccuracy", "\u2014", false);
            StatisticsSync.Accuracy["A"] = new ShotAccuracy(4, 0);
            Refresh(ownEntry, thirdEntry, live);
            Accuracy(ownEntry, "bloodmoonGroupAccuracy", "0 %", true);
            Accuracy(thirdEntry, "bloodmoonGroupAccuracy", "0 %", true);
            Accuracy(live, "liveAccuracy", "0 %", true);

            a.Party.MemberList.Remove(c);
            StatisticsSync.Accuracy["A"] = new ShotAccuracy(4, 1);
            StatisticsSync.Accuracy["C"] = new ShotAccuracy(4, 4);
            Refresh(ownEntry, live);
            Accuracy(ownEntry, "bloodmoonGroupAccuracy", "25 %", true);
            Accuracy(live, "liveAccuracy", "25 %", true);
            Need(GroupAccuracyRanking.Maximum(a) == 25m, "removed party members do not affect ranking");
            a.Party = null;
            Need(GroupAccuracyRanking.Maximum(a) == 25m && GroupAccuracyRanking.Maximum(null) < 0, "solo and missing local player ranking");
            Refresh(live);
            Accuracy(live, "liveAccuracy", "25 %", true);

            Label(ownEntry, "bloodmoonGroupKills").UiTransform.Rendered.printedSize = new UnityEngine.Vector2 { x = 100, y = 26 };
            Label(live, "liveKills").UiTransform.Rendered.printedSize = new UnityEngine.Vector2 { x = 110, y = 36 };
            Refresh(ownEntry, live);
            Need(Label(ownEntry, "bloodmoonGroupAccuracy").Position.x > ownX, "party percentage moves right for wider kill text");
            Need(Label(ownEntry, "bloodmoonGroupAccuracy").Position.x >= Star(ownEntry).Position.x + 26, "wide kills star and percentage stay separate");
            Need(Label(live, "liveAccuracy").Position.x >= 110 + 8, "standalone percentage moves right for wider kill text");
            Label(ownEntry, "bloodmoonGroupKills").UiTransform = null;
            Label(live, "liveKills").UiTransform = null;
            Refresh(ownEntry, live);
            Need(Label(ownEntry, "bloodmoonGroupAccuracy").IsVisible && Label(live, "liveAccuracy").IsVisible, "missing rendered label is handled safely");

            StatisticsSync.Kills.Remove("A");
            Refresh(ownEntry, live);
            Need(!Label(ownEntry, "bloodmoonGroupKills").IsVisible && !Label(ownEntry, "bloodmoonGroupAccuracy").IsVisible && !Star(ownEntry).IsVisible, "hidden party kills hide accuracy and star");
            Need(!Label(live, "liveKills").IsVisible && !Label(live, "liveAccuracy").IsVisible, "hidden solo kills hide accuracy");
            Need(!Label(live, "liveHeadshots").IsVisible && !Label(ownEntry, "bloodmoonGroupHeadshots").IsVisible, "headshots hidden with HUD");
            Need(!((XUiV_Sprite)live.GetChildById("liveHeadshotsIcon").ViewComponent).IsVisible, "headshot icon hidden");
            otherEntry.Player = null; live.xui.playerUI.entityPlayer = null;
            Refresh(otherEntry, live);
            Need(!Label(otherEntry, "bloodmoonGroupAccuracy").IsVisible && !Label(live, "liveAccuracy").IsVisible, "missing players hide recycled HUD labels");
            StatisticsSync.Kills["A"] = 1;
            StatisticsSync.Accuracy.Remove("A");
            otherEntry.Player = a;
            Refresh(otherEntry);
            Accuracy(otherEntry, "bloodmoonGroupAccuracy", "\u2014", false);
            Console.WriteLine("PASS: own/party HUD accuracy, green winners and ties, displayed rounding, missing data, zero hits, membership, recycled labels, kill colors/stars, adaptive layout and visibility.");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
