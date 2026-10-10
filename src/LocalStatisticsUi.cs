using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;
using HarmonyLib;

namespace ModStatistik
{
    internal static class LocalStatisticsUi
    {
        internal static void Apply(XElement root, string name)
        {
            if (GameManager.IsDedicatedServer || root == null) return;
            string marker = name == "windows" ? "bloodmoonStatistics" :
                name == "templates" ? "bloodmoonGroupKills" : "windowBloodmoonKills";
            if (root.Descendants().Any(e => (string)e.Attribute("name") == marker)) return;
            // The patches live inside the DLL, not in Config/XUi_InGame. A server
            // therefore has no UI definitions to push to clients without the DLL.
            using (var stream = typeof(LocalStatisticsUi).Assembly.GetManifestResourceStream(
                "ModStatistik.LocalUI." + name + ".xml"))
            {
                if (stream == null) throw new InvalidDataException("Lokale Statistikoberflaeche fehlt.");
                var patch = XDocument.Load(stream);
                if (root.Document == null) new XDocument(root);
                foreach (var operation in patch.Root.Elements())
                {
                    string xpath = (string)operation.Attribute("xpath");
                    if (operation.Name.LocalName == "set")
                    {
                        int slash = xpath.LastIndexOf("/@", StringComparison.Ordinal);
                        if (slash < 0) throw new InvalidDataException("Ungueltiges UI-Attribut.");
                        string attribute = xpath.Substring(slash + 2);
                        foreach (var target in root.XPathSelectElements(xpath.Substring(0, slash)).ToArray())
                            target.SetAttributeValue(attribute, operation.Value);
                    }
                    else
                    {
                        foreach (var target in root.XPathSelectElements(xpath).ToArray())
                        {
                            var nodes = operation.Elements().Select(e => new XElement(e)).ToArray();
                            if (operation.Name.LocalName == "append") target.Add(nodes);
                            else if (operation.Name.LocalName == "insertAfter") target.AddAfterSelf(nodes);
                            else throw new InvalidDataException("Unbekannte UI-Operation.");
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(XUiFromXml), "Load")]
    internal static class LocalStatisticsXuiPatch
    {
        private static void Prefix(ref XmlFile __0)
        {
            if (GameManager.IsDedicatedServer || __0 == null || __0.XmlDoc == null) return;
            var original = __0;
            __0 = (XmlFile)typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(original, null);
            __0.XmlDoc = new XDocument(original.XmlDoc);
            LocalStatisticsUi.Apply(__0.XmlDoc.Root, "xui");
        }
    }

    [HarmonyPatch(typeof(XUiFromXml), "loadWindows")]
    internal static class LocalStatisticsWindowsPatch
    {
        private static void Prefix(ref XElement __0)
        {
            if (GameManager.IsDedicatedServer || __0 == null) return;
            __0 = new XDocument(new XElement(__0)).Root;
            LocalStatisticsUi.Apply(__0, "windows");
        }
    }

    [HarmonyPatch(typeof(XUiFromXml), "loadTemplates")]
    internal static class LocalStatisticsTemplatesPatch
    {
        private static void Prefix(ref XElement __0)
        {
            if (GameManager.IsDedicatedServer || __0 == null) return;
            __0 = new XDocument(new XElement(__0)).Root;
            LocalStatisticsUi.Apply(__0, "templates");
        }
    }
}
