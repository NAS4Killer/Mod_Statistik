using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Runtime.CompilerServices;
using HarmonyLib;
using ModStatistik;

public static class GameManager { public static bool IsDedicatedServer; }
public class XmlFile { public XDocument XmlDoc; }
public static class XUiFromXml
{
    public static XElement Observed;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IEnumerator Load(XmlFile file) { Observed = file.XmlDoc.Root; return new object[0].GetEnumerator(); }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void loadWindows(XElement root) { Observed = root; }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void loadTemplates(XElement root) { Observed = root; }
    public static void Windows(XElement root) { loadWindows(root); }
    public static void Templates(XElement root) { loadTemplates(root); }
}

class LocalUiTests
{
    private static void Need(bool value, string message) { if (!value) throw new Exception(message); }
    static int Main(string[] args)
    {
        try
        {
            new Harmony("NAS4Killer.ModStatistik.LocalUiTests").PatchAll(typeof(LocalStatisticsUi).Assembly);
            var windows = XDocument.Load(Path.Combine(args[0], "windows.xml"));
            var templates = XDocument.Load(Path.Combine(args[0], "templates.xml"));
            var xui = new XmlFile { XmlDoc = XDocument.Load(Path.Combine(args[0], "xui.xml")) };
            string initialWindows = windows.ToString(), initialTemplates = templates.ToString(), initialXui = xui.XmlDoc.ToString();
            GameManager.IsDedicatedServer = true;
            XUiFromXml.Windows(windows.Root); Need(Object.ReferenceEquals(XUiFromXml.Observed, windows.Root), "dedicated server windows untouched");
            XUiFromXml.Templates(templates.Root); Need(Object.ReferenceEquals(XUiFromXml.Observed, templates.Root), "dedicated server templates untouched");
            XUiFromXml.Load(xui); Need(Object.ReferenceEquals(XUiFromXml.Observed, xui.XmlDoc.Root), "dedicated server xui untouched");
            GameManager.IsDedicatedServer = false;
            XUiFromXml.Windows(windows.Root);
            var localWindows = XUiFromXml.Observed;
            Need(localWindows.Descendants().Count(e => (string)e.Attribute("name") == "bloodmoonStatistics") == 1, "local tab added");
            Need(localWindows.Descendants().Count(e => (string)e.Attribute("name") == "windowBloodmoonKills") == 1, "local kills window added");
            var killsWindow = localWindows.Descendants("window").Single(e => (string)e.Attribute("name") == "windowBloodmoonKills");
            Need(killsWindow.Elements("label").Count(e => (string)e.Attribute("name") == "liveAccuracy") == 1, "own HUD accuracy label added once");
            Need((string)killsWindow.Elements("label").Single(e => (string)e.Attribute("name") == "liveKills").Attribute("color") == "255,0,0,255", "own kill color preserved");
            Need((string)killsWindow.Elements("label").Single(e => (string)e.Attribute("name") == "liveAccuracy").Attribute("visible") == "false", "own accuracy initially hidden");
            Need(killsWindow.Elements("label").Count(e => (string)e.Attribute("name") == "liveHeadshots") == 1, "headshot HUD added");
            string[] kinds = { "Kills", "Accuracy", "Headshots" };
            string[] sprites = { "ui_game_symbol_death", "ui_game_symbol_twitch_boss_weapon_arrows_overhead", "ui_game_symbol_twitch_boss_overhead" };
            for (int i = 0; i < kinds.Length; i++)
                Need((string)killsWindow.Elements("sprite").Single(e => (string)e.Attribute("name") == "live" + kinds[i] + "Icon").Attribute("sprite") == sprites[i], "chosen HUD icon " + kinds[i]);
            var grid = localWindows.Descendants("grid").Single(e => (string)e.Attribute("name") == "statisticsRows");
            Need((int)grid.Attribute("rows") * (int)grid.Attribute("cell_height") <= (int)grid.Attribute("height"), "all player rows fit above countdown");
            var row = grid.Element("rect");
            for (int i = 0; i < 11; i++)
            {
                Need(row.Elements("label").Count(e => (string)e.Attribute("name") == "headshots" + i) == 1, "headshot line in every night column");
                Need((string)row.Elements("sprite").Single(e => (string)e.Attribute("name") == "headshotsIcon" + i).Attribute("sprite") == sprites[2], "chosen headshot table icon");
            }
            Need(windows.ToString() == initialWindows, "original shared windows unchanged");
            XUiFromXml.Windows(localWindows);
            Need(XUiFromXml.Observed.Descendants().Count(e => (string)e.Attribute("name") == "bloodmoonStatistics") == 1, "windows not duplicated");
            Need(XUiFromXml.Observed.Descendants().Count(e => (string)e.Attribute("name") == "liveAccuracy") == 1, "own HUD accuracy not duplicated");
            XUiFromXml.Templates(templates.Root);
            var localTemplates = XUiFromXml.Observed;
            var party = localTemplates.XPathSelectElement("/templates/party_entry/rect");
            Need((string)party.Attribute("controller") == "StatistikPartyEntry, Mod_Statistik", "native group controller extended");
            Need(party.Elements().Count(e => (string)e.Attribute("name") == "bloodmoonGroupKills") == 1, "group count added");
            Need(party.Elements("label").Count(e => (string)e.Attribute("name") == "bloodmoonGroupAccuracy") == 1, "group accuracy label added once");
            Need((string)party.Elements("label").Single(e => (string)e.Attribute("name") == "bloodmoonGroupAccuracy").Attribute("visible") == "false", "group accuracy initially hidden");
            Need((string)party.Elements("sprite").Single(e => (string)e.Attribute("name") == "bloodmoonGroupStar").Attribute("color") == "255,0,0,255", "group kill star remains red");
            for (int i = 0; i < kinds.Length; i++)
                Need((string)party.Elements("sprite").Single(e => (string)e.Attribute("name") == "bloodmoonGroup" + kinds[i] + "Icon").Attribute("sprite") == sprites[i], "chosen party icon " + kinds[i]);
            Need((string)party.Elements("label").Single(e => (string)e.Attribute("text") == "{distance}").Attribute("width") == "112", "missing distance attribute added");
            Need(templates.ToString() == initialTemplates, "original shared templates unchanged");
            XUiFromXml.Templates(localTemplates);
            Need(XUiFromXml.Observed.Descendants().Count(e => (string)e.Attribute("name") == "bloodmoonGroupKills") == 1, "group not duplicated");
            Need(XUiFromXml.Observed.Descendants().Count(e => (string)e.Attribute("name") == "bloodmoonGroupAccuracy") == 1, "group accuracy not duplicated");
            XUiFromXml.Load(xui);
            Need(XUiFromXml.Observed.Descendants().Any(e => (string)e.Attribute("name") == "windowBloodmoonKills"), "HUD window linked locally");
            Need(xui.XmlDoc.ToString() == initialXui, "original shared xui unchanged");
            Console.WriteLine("PASS: dedicated server UI unchanged; client tab, own/party HUD accuracy and red kill markers; originals preserved for unmodded clients and hosted games; idempotent loading.");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
