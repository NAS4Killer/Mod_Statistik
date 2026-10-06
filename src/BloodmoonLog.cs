using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace ModStatistik
{
    public sealed class BloodmoonNight
    {
        public int Day { get; private set; }
        public IReadOnlyDictionary<string, long> Kills { get; private set; }
        public long Total { get { return Kills.Values.Sum(); } }

        internal BloodmoonNight(int day, Dictionary<string, long> kills)
        {
            Day = day;
            Kills = new System.Collections.ObjectModel.ReadOnlyDictionary<string, long>(kills);
        }
    }

    public sealed class BloodmoonLog
    {
        private static readonly Regex End = new Regex(@"^=== Blutmond Tag (\d+) END ", RegexOptions.CultureInvariant);
        private static readonly Regex Record = new Regex(@"^(.*?) start=(-?\d+|None) end=(-?\d+|None) bm_kills=(\d+)$", RegexOptions.CultureInvariant);
        public IReadOnlyList<BloodmoonNight> Nights { get; private set; }
        public IReadOnlyList<string> Players { get; private set; }

        public static BloodmoonLog Parse(string text)
        {
            if (text == null) throw new ArgumentNullException("text");
            var nights = new Dictionary<int, Dictionary<string, long>>();
            Dictionary<string, long> current = null;
            bool recognized = false;
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                if (line.StartsWith("===", StringComparison.Ordinal))
                {
                    recognized |= line.StartsWith("=== Blutmond Tag ", StringComparison.Ordinal);
                    current = null;
                    Match end = End.Match(line);
                    int day;
                    if (end.Success && int.TryParse(end.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out day) && day > 0 && !nights.ContainsKey(day))
                    {
                        current = new Dictionary<string, long>(StringComparer.Ordinal);
                        nights.Add(day, current);
                    }
                    continue;
                }
                if (current == null || string.IsNullOrWhiteSpace(line) || line == "(keine Spieler)") continue;
                Match record = Record.Match(line);
                long kills;
                if (!record.Success || !long.TryParse(record.Groups[4].Value, NumberStyles.None, CultureInfo.InvariantCulture, out kills))
                    throw new FormatException("Ungueltiger Eintrag in einer abgeschlossenen Blutmondnacht.");
                string name = record.Groups[1].Value.Trim();
                if (name.Length == 0) throw new FormatException("Spielername fehlt.");
                if (name == "EntityPlayer" || name == "Spieler online" || name == "Spieler offline") continue;
                if (current.ContainsKey(name)) throw new FormatException("Doppelter Spieler in derselben Blutmondnacht.");
                current.Add(name, kills);
            }
            if (!recognized) throw new FormatException("Keine Blutmond-Protokolldatei erkannt.");
            var result = new BloodmoonLog();
            result.Nights = nights.OrderByDescending(n => n.Key).Select(n => new BloodmoonNight(n.Key, n.Value)).ToList().AsReadOnly();
            result.Players = nights.Values.SelectMany(n => n.Keys).Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
            foreach (var night in result.Nights) { long total = night.Total; }
            return result;
        }
    }
}
