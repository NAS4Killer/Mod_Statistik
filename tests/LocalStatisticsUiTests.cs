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
            Need(windows.ToString() == initialWindows, "original shared windows unchanged");
            XUiFromXml.Windows(localWindows);
            Need(XUiFromXml.Observed.Descendants().Count(e => (string)e.Attribute("name") == "bloodmoonStatistics") == 1, "windows not duplicated");
            XUiFromXml.Templates(templates.Root);
            var localTemplates = XUiFromXml.Observed;
            var party = localTemplates.XPathSelectElement("/templates/party_entry/rect");
            Need((string)party.Attribute("controller") == "StatistikPartyEntry, Mod_Statistik", "native group controller extended");
            Need(party.Elements().Count(e => (string)e.Attribute("name") == "bloodmoonGroupKills") == 1, "group count added");
            Need((string)party.Elements("label").Single(e => (string)e.Attribute("text") == "{distance}").Attribute("width") == "112", "missing distance attribute added");
            Need(templates.ToString() == initialTemplates, "original shared templates unchanged");
            XUiFromXml.Templates(localTemplates);
            Need(XUiFromXml.Observed.Descendants().Count(e => (string)e.Attribute("name") == "bloodmoonGroupKills") == 1, "group not duplicated");
            XUiFromXml.Load(xui);
            Need(XUiFromXml.Observed.Descendants().Any(e => (string)e.Attribute("name") == "windowBloodmoonKills"), "HUD window linked locally");
            Need(xui.XmlDoc.ToString() == initialXui, "original shared xui unchanged");
            Console.WriteLine("PASS: dedicated server UI unchanged; client tab, own HUD and native group extension; originals preserved for unmodded clients and hosted games; idempotent loading.");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
