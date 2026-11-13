using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ModStatistik;

internal static class StatisticsAccuracyWireTests
{
    private static int checks;
    private static readonly string Session = "1234567890abcdef1234567890abcdef";

    private static void Check(bool condition, string name)
    {
        checks++;
        if (!condition) throw new Exception(name);
    }

    private static void Reject(Action action, string name)
    {
        checks++;
        try { action(); }
        catch (InvalidDataException) { return; }
        catch (EndOfStreamException) { return; }
        catch (FormatException) { return; }
        catch (ArgumentException) { return; }
        catch (OverflowException) { return; }
        throw new Exception("Angenommen: " + name);
    }

    private static BloodmoonLog Log(params BloodmoonNight[] nights)
    { return BloodmoonLog.FromNights(nights); }

    private static BloodmoonNight Night(int day, long shots, long hits)
    {
        return new BloodmoonNight(day, new Dictionary<string, long> { { "A", 5 } },
            new Dictionary<string, ShotAccuracy> { { "A", new ShotAccuracy(shots, hits) } });
    }

    private static byte[] Change(byte[] bytes, int position, byte value)
    { byte[] copy = (byte[])bytes.Clone(); copy[position] = value; return copy; }

    private static byte[] ChangeInt(byte[] bytes, int position, int value)
    { byte[] copy = (byte[])bytes.Clone(); Buffer.BlockCopy(BitConverter.GetBytes(value), 0, copy, position, 4); return copy; }

    private static byte[] ChangeLong(byte[] bytes, int position, long value)
    { byte[] copy = (byte[])bytes.Clone(); Buffer.BlockCopy(BitConverter.GetBytes(value), 0, copy, position, 8); return copy; }

    private static string Message(byte[] bytes)
    { return StatisticsMessage.Prefix + Convert.ToBase64String(bytes); }

    private static byte[] MessageBytes(string message)
    { return Convert.FromBase64String(message.Substring(StatisticsMessage.Prefix.Length)); }

    private static void LegacyAndAccuracy()
    {
        BloodmoonLog source = Log(Night(14, 10, 7));
        int active;
        byte[] legacy = StatisticsWire.Encode(source);
        BloodmoonLog decoded = StatisticsWire.Decode(legacy, out active);
        Check(legacy[0] == 1 && active == 0 && decoded.Nights[0].Kills["A"] == 5 &&
            decoded.Nights[0].Accuracy.Count == 0, "Altes Format 1 unveraendert");
        legacy = StatisticsWire.Encode(source, 14);
        decoded = StatisticsWire.Decode(legacy, out active);
        Check(legacy[0] == 2 && active == 14 && decoded.Nights[0].Accuracy.Count == 0, "Altes Format 2 unveraendert");

        var night = new BloodmoonNight(14,
            new Dictionary<string, long> { { "Unknown", 1 }, { "NoShots", 0 }, { "Missed", 0 }, { "Accurate", 1 }, { "T\u00e4st", 2 } },
            new Dictionary<string, ShotAccuracy> { { "NoShots", new ShotAccuracy(0, 0) }, { "Missed", new ShotAccuracy(5, 0) },
                { "Accurate", new ShotAccuracy(3, 2) }, { "T\u00e4st", new ShotAccuracy(StatisticsWire.MaximumShots, StatisticsWire.MaximumShots) } });
        byte[] encoded = StatisticsWire.EncodeAccuracy(Log(night), 14);
        decoded = StatisticsWire.Decode(encoded, out active);
        Check(encoded[0] == 3 && active == 14, "Trefferformat mit aktivem Tag");
        Check(!decoded.Nights[0].Accuracy.ContainsKey("Unknown"), "Unbekannte Trefferquote bleibt unbekannt");
        Check(decoded.Nights[0].Accuracy["NoShots"].Shots == 0 && decoded.Nights[0].Accuracy["NoShots"].Hits == 0, "Bekannt ohne Schuesse");
        Check(decoded.Nights[0].Accuracy["Missed"].Shots == 5 && decoded.Nights[0].Accuracy["Missed"].Hits == 0, "Null Prozent bleibt messbar");
        Check(decoded.Nights[0].Accuracy["Accurate"].Shots == 3 && decoded.Nights[0].Accuracy["Accurate"].Hits == 2, "Zaehlwerte erhalten");
        Check(decoded.Nights[0].Accuracy["T\u00e4st"].Hits == StatisticsWire.MaximumShots, "Unicode und obere Grenze");
        Check(StatisticsWire.Equal(encoded, StatisticsWire.EncodeAccuracy(decoded, active)), "Deterministische Kodierung");

        byte[] single = StatisticsWire.EncodeAccuracy(source, 14);
        for (int i = 0; i < single.Length; i++)
        {
            byte[] truncated = single.Take(i).ToArray();
            Reject(() => StatisticsWire.Decode(truncated), "Gekuerzte Trefferstatistik " + i);
        }
        Reject(() => StatisticsWire.Decode(single.Concat(new byte[] { 0 }).ToArray()), "Zusaetzliche Statistikdaten");
        Reject(() => StatisticsWire.Decode(Change(single, 0, 4)), "Unbekanntes Format");
        Reject(() => StatisticsWire.Decode(ChangeInt(single, 1, -1)), "Negativer aktiver Tag");
        Reject(() => StatisticsWire.Decode(ChangeInt(single, 1, 21)), "Aktiver Tag fehlt");
        Reject(() => StatisticsWire.Decode(ChangeInt(single, 7, 0)), "Tag Null");
        Reject(() => StatisticsWire.Decode(Change(single, 23, 2)), "Ungueltiges Verfuegbarkeitsflag");
        Reject(() => StatisticsWire.Decode(ChangeLong(single, 24, -1)), "Negative Schuesse");
        Reject(() => StatisticsWire.Decode(ChangeLong(single, 32, -1)), "Negative Treffer");
        Reject(() => StatisticsWire.Decode(ChangeLong(single, 32, 11)), "Mehr Treffer als Schuesse");
        Reject(() => StatisticsWire.Decode(ChangeLong(single, 24, StatisticsWire.MaximumShots + 1)), "Schussgrenze");
        Reject(() => StatisticsWire.Decode(ChangeLong(single, 15, -1)), "Negative Kills");
        Reject(() => StatisticsWire.Decode(Change(single, 13, 0)), "Leerer Name");
        Reject(() => StatisticsWire.Decode(Change(single, 14, 255)), "Ungueltiges UTF8");
        Reject(() => StatisticsWire.Decode(new byte[StatisticsWire.MaximumBytes + 1]), "Paket zu gross");
        Reject(() => StatisticsWire.EncodeAccuracy(source, -1), "Trefferformat ohne aktiven Tag");
        Reject(() => StatisticsWire.EncodeAccuracy(source, 21), "Fehlender aktiver Tag beim Schreiben");
        Reject(() => StatisticsWire.EncodeAccuracy(Log(Night(14, StatisticsWire.MaximumShots + 1, 0)), 0), "Schussgrenze beim Schreiben");
        Reject(() => StatisticsWire.EncodeAccuracy(Log(Night(14, 0, 0), Night(14, 0, 0)), 0), "Doppelter Tag");
        Reject(() => StatisticsWire.EncodeAccuracy(Log(Night(0, 0, 0)), 0), "Tag Null beim Schreiben");
        var orphan = new BloodmoonNight(14, new Dictionary<string, long>(),
            new Dictionary<string, ShotAccuracy> { { "A", new ShotAccuracy(1, 1) } });
        Reject(() => StatisticsWire.EncodeAccuracy(Log(orphan), 0), "Trefferwert ohne Killspalte");
        var missing = new BloodmoonNight(14, new Dictionary<string, long> { { "A", 0 } },
            new Dictionary<string, ShotAccuracy> { { "A", null } });
        Reject(() => StatisticsWire.EncodeAccuracy(Log(missing), 0), "Null-Trefferwert");
        var overflow = new BloodmoonNight(14, new Dictionary<string, long> { { "A", long.MaxValue }, { "B", 1 } });
        Reject(() => StatisticsWire.EncodeAccuracy(Log(overflow), 0), "Summenueberlauf");
        Reject(() => StatisticsWire.EncodeAccuracy(Log(Enumerable.Range(1, 513).Select(d => Night(d, 0, 0)).ToArray()), 0), "Nachtgrenze");
        var manyPlayers = Enumerable.Range(0, 129).ToDictionary(i => "P" + i, i => 0L);
        Reject(() => StatisticsWire.EncodeAccuracy(Log(new BloodmoonNight(14, manyPlayers)), 0), "Spielergrenze");
        var maximumPlayers = Enumerable.Range(0, 128).ToDictionary(i => "Player" + i, i => 0L);
        var oversized = Log(Enumerable.Range(1, 512).Select(d => new BloodmoonNight(d, maximumPlayers)).ToArray());
        Reject(() => StatisticsWire.EncodeAccuracy(oversized, 0), "Gesamtgroesse beim Schreiben");
    }

    private static void Messages()
    {
        byte[] hash = StatisticsWire.Hash(new byte[] { 4 });
        string request = StatisticsMessage.Request(9, hash);
        StatisticsMessage frame = StatisticsMessage.Decode(request);
        Check(MessageBytes(request)[0] == 1 && frame.IsRequest && !frame.Extended && frame.Report == null && frame.Id == 9 &&
            StatisticsWire.Equal(frame.Hash, hash), "Alte Anfrage unveraendert");
        var report = new AccuracyReport { Day = 14, Session = Session, Shots = 9, Hits = 4 };
        request = StatisticsMessage.RequestExtended(10, hash, report);
        frame = StatisticsMessage.Decode(request);
        Check(MessageBytes(request)[0] == 3 && frame.IsRequest && frame.Extended && frame.Id == 10 &&
            frame.Report.Day == 14 && frame.Report.Session == Session && frame.Report.Shots == 9 && frame.Report.Hits == 4 &&
            !frame.Report.Final && StatisticsWire.Equal(frame.Hash, hash), "Erweiterte Anfrage mit kumulativer Meldung");
        report.Final = true;
        frame = StatisticsMessage.Decode(StatisticsMessage.RequestExtended(10, hash, report));
        Check(frame.Report.Final && frame.Report.Shots == 9 && frame.Report.Hits == 4, "Abschlussmeldung mit Zaehlern");
        frame = StatisticsMessage.Decode(StatisticsMessage.RequestExtended(1, null, null));
        Check(frame.Extended && frame.IsRequest && frame.Report == null && frame.Hash.All(b => b == 0), "Anfrage ohne Trefferbericht");
        frame = StatisticsMessage.Decode(StatisticsMessage.RequestExtended(1, null,
            new AccuracyReport { Day = 14, Session = Session.ToUpperInvariant(), Shots = 0, Hits = 0 }));
        Check(frame.Report.Session == Session && frame.Report.Shots == 0, "Leerer Bericht und normalisierte Sitzung");

        byte[] bytes = MessageBytes(request);
        for (int i = 0; i < bytes.Length; i++)
        {
            string truncated = Message(bytes.Take(i).ToArray());
            Reject(() => StatisticsMessage.Decode(truncated), "Gekuerzte Anfrage " + i);
        }
        Reject(() => StatisticsMessage.Decode(Message(bytes.Concat(new byte[] { 0 }).ToArray())), "Anfrage mit Anhang");
        Reject(() => StatisticsMessage.Decode(Message(Change(bytes, 0, 5))), "Unbekannte Nachricht");
        Reject(() => StatisticsMessage.Decode(Message(ChangeInt(bytes, 1, 0))), "Anfragekennung Null");
        Reject(() => StatisticsMessage.Decode(Message(Change(bytes, 37, 2))), "Ungueltiges Berichtsflag");
        Reject(() => StatisticsMessage.Decode(Message(Change(bytes, 74, 2))), "Ungueltiges Abschlussflag");
        Reject(() => StatisticsMessage.Decode(Message(ChangeInt(bytes, 38, 0))), "Berichtstag Null");
        Reject(() => StatisticsMessage.Decode(Message(ChangeLong(bytes, 58, -1))), "Negativer Bericht");
        Reject(() => StatisticsMessage.Decode(Message(ChangeLong(bytes, 66, 10))), "Zu viele Berichtstreffer");
        Reject(() => StatisticsMessage.Decode(Message(ChangeLong(bytes, 58, StatisticsWire.MaximumShots + 1))), "Berichtsgrenze");
        byte[] emptySession = (byte[])bytes.Clone(); Array.Clear(emptySession, 42, 16);
        Reject(() => StatisticsMessage.Decode(Message(emptySession)), "Leere Sitzung");
        Reject(() => StatisticsMessage.RequestExtended(1, hash, new AccuracyReport { Day = 1, Session = "invalid" }), "Sitzung beim Schreiben");
        Reject(() => StatisticsMessage.RequestExtended(1, hash, new AccuracyReport { Day = 1, Session = Guid.Empty.ToString("N") }), "Leere Sitzung beim Schreiben");
        Reject(() => StatisticsMessage.RequestExtended(1, new byte[31], null), "Kurze Historienpruefsumme");
        Reject(() => StatisticsMessage.Decode(StatisticsMessage.Prefix + new string('A', StatisticsMessage.MaximumTextLength)), "Textgrenze");
        Reject(() => StatisticsMessage.Decode("chat"), "Normale Chatnachricht");

        byte[] payload = Enumerable.Range(0, StatisticsWire.ChunkBytes + 21).Select(i => (byte)i).ToArray();
        byte[] digest = StatisticsWire.Hash(payload);
        var transfer = new StatisticsTransfer();
        for (int i = 1; i >= 0; i--)
        {
            byte[] chunk = payload.Skip(i * StatisticsWire.ChunkBytes).Take(StatisticsWire.ChunkBytes).ToArray();
            frame = StatisticsMessage.Decode(StatisticsMessage.ReplyExtended(12, 0, i, 2, payload.Length, digest, chunk));
            Check(!frame.IsRequest && frame.Extended && frame.Id == 12, "Erweiterte Antwort");
            byte[] assembled = transfer.Accept(frame.Id, frame.Index, frame.Count, frame.Total, frame.Hash, frame.Chunk);
            Check(i == 1 ? assembled == null : StatisticsWire.Equal(assembled, payload), "Teilpakete ausser Reihenfolge");
        }
        frame = StatisticsMessage.Decode(StatisticsMessage.Reply(1, 1, 0, 0, 0, hash, new byte[0]));
        Check(!frame.Extended && !frame.IsRequest && frame.Result == 1, "Alte Antwort unveraendert");
        foreach (byte result in new byte[] { 1, 2 })
        {
            string reply = StatisticsMessage.ReplyExtended(1, result, 0, 0, 0, hash, new byte[0]);
            frame = StatisticsMessage.Decode(reply);
            Check(frame.Extended && frame.Result == result, "Erweiterter Status " + result);
            Reject(() => StatisticsMessage.Decode(Message(MessageBytes(reply).Concat(new byte[] { 0 }).ToArray())), "Antwortanhang");
        }
        Reject(() => StatisticsMessage.ReplyExtended(1, 0, 0, 1, 9, hash, new byte[8]), "Falsche Teilpaketgroesse");
        Reject(() => StatisticsMessage.ReplyExtended(1, 1, 0, 0, 1, hash, new byte[0]), "Status mit Datenlaenge");
        Reject(() => StatisticsMessage.ReplyExtended(1, 0, 0, 33, StatisticsWire.MaximumBytes + 1, hash, new byte[8192]), "Antwortgrenze");
    }

    private static void Deltas()
    {
        byte[] history = StatisticsWire.EncodeAccuracy(Log(Night(7, 100, 80)), 0);
        byte[] hash = StatisticsWire.Hash(history);
        byte[] live = StatisticsWire.EncodeAccuracy(Log(Night(14, 10, 6)), 14);
        byte[] encoded = StatisticsDelta.Encode(history, hash, live, true);
        StatisticsDelta delta = StatisticsDelta.Decode(encoded);
        Check(StatisticsWire.Equal(delta.HistoryPayload, history) && StatisticsWire.Equal(delta.HistoryHash, hash) &&
            StatisticsWire.Equal(delta.LivePayload, live), "Erste Antwort mit Historie und Livewerten");
        byte[] incremental = StatisticsDelta.Encode(history, hash, live, false);
        delta = StatisticsDelta.Decode(incremental);
        Check(delta.HistoryPayload == null && StatisticsWire.Equal(delta.HistoryHash, hash) &&
            StatisticsWire.Equal(delta.LivePayload, live), "Folgeantwort mit Historienkennung und Livewerten");
        byte[] bigHistory = StatisticsWire.EncodeAccuracy(Log(Enumerable.Range(1, 512).Select(d => Night(d, 10000, 7000)).ToArray()), 0);
        byte[] bigIncremental = StatisticsDelta.Encode(bigHistory, StatisticsWire.Hash(bigHistory), live, false);
        Check(bigIncremental.Length == incremental.Length && bigIncremental.Length == 41 + live.Length,
            "Liveverkehr unabhaengig von 1 oder 512 historischen Naechten");
        Check(StatisticsDelta.Encode(null, hash, live, false).Length == incremental.Length, "Historienbytes bei Folgeantwort nicht erforderlich");
        byte[] empty = StatisticsWire.EncodeAccuracy(Log(), 0);
        delta = StatisticsDelta.Decode(StatisticsDelta.Encode(empty, StatisticsWire.Hash(empty), empty, true));
        Check(delta.LivePayload.Length == empty.Length && delta.HistoryPayload.Length == empty.Length, "Leere Historie und keine aktive Nacht");

        for (int i = 0; i < encoded.Length; i++)
        {
            byte[] truncated = encoded.Take(i).ToArray();
            Reject(() => StatisticsDelta.Decode(truncated), "Gekuerzte Aktualisierung " + i);
        }
        Reject(() => StatisticsDelta.Decode(encoded.Concat(new byte[] { 0 }).ToArray()), "Aktualisierungsanhang");
        Reject(() => StatisticsDelta.Decode(Change(encoded, 0, 2)), "Unbekannte Aktualisierung");
        Reject(() => StatisticsDelta.Decode(Change(encoded, 1, (byte)(encoded[1] ^ 1))), "Manipulierte Historienpruefsumme");
        Reject(() => StatisticsDelta.Decode(ChangeInt(encoded, 33, -1)), "Negative Historiengroesse");
        Reject(() => StatisticsDelta.Decode(ChangeInt(encoded, 33, int.MaxValue)), "Uebergrosse Historienangabe");
        Reject(() => StatisticsDelta.Decode(ChangeInt(encoded, 37 + history.Length, 0)), "Leere Livegroesse");
        Reject(() => StatisticsDelta.Decode(ChangeInt(encoded, 37 + history.Length, int.MaxValue)), "Uebergrosse Liveangabe");
        Reject(() => StatisticsDelta.Decode(new byte[StatisticsWire.MaximumBytes + 1]), "Aktualisierungsgrenze");
        Reject(() => StatisticsDelta.Encode(history, new byte[32], live, true), "Falscher Historienhash beim Schreiben");
        Reject(() => StatisticsDelta.Encode(history, new byte[31], live, false), "Falsche Hashgroesse");
        Reject(() => StatisticsDelta.Encode(null, hash, live, true), "Fehlende angeforderte Historie");
        Reject(() => StatisticsDelta.Encode(history, hash, new byte[0], false), "Leere Livebytes");
        Reject(() => StatisticsDelta.Encode(history, hash, StatisticsWire.Encode(Log(), 0), false), "Altes Liveformat");
        Reject(() => StatisticsDelta.Encode(history, hash, history, false), "Abgeschlossene Nacht als Livewert");
        byte[] twoLive = StatisticsWire.EncodeAccuracy(Log(Night(7, 1, 1), Night(14, 2, 1)), 14);
        Reject(() => StatisticsDelta.Encode(history, hash, twoLive, false), "Mehrere Livenaechte");
        Reject(() => StatisticsDelta.Encode(live, StatisticsWire.Hash(live), empty, true), "Aktive Nacht in Historie");
        byte[] legacy = StatisticsWire.Encode(Log());
        Reject(() => StatisticsDelta.Encode(legacy, StatisticsWire.Hash(legacy), empty, true), "Altes Historienformat");

        // Valid history can fit the wire limit while its envelope no longer fits.
        // Exercise that combined boundary with real decodable records.
        var players = Enumerable.Range(0, 128).ToDictionary(i => "Player" + i.ToString("D10"), i => 0L);
        var nearLimitNights = Enumerable.Range(1, 78).Select(d => new BloodmoonNight(d, players)).ToList();
        nearLimitNights.Add(new BloodmoonNight(79, players.Take(78).ToDictionary(p => p.Key, p => p.Value)));
        byte[] nearLimit = StatisticsWire.EncodeAccuracy(BloodmoonLog.FromNights(nearLimitNights), 0);
        byte[] nearLimitHash = StatisticsWire.Hash(nearLimit);
        byte[] nearLimitDelta = StatisticsDelta.Encode(nearLimit, nearLimitHash, empty, true);
        Check(nearLimitDelta.Length == StatisticsWire.MaximumBytes - 3 &&
            StatisticsDelta.Decode(nearLimitDelta).HistoryPayload.Length == nearLimit.Length, "Gueltige Aktualisierung knapp unter Groessengrenze");
        Reject(() => StatisticsDelta.Encode(nearLimit, nearLimitHash, live, true), "Historie plus Livewerte ueber Gesamtgrenze");
    }

    private static int Main()
    {
        try
        {
            LegacyAndAccuracy(); Messages(); Deltas();
            Console.WriteLine("Trefferprotokoll: " + checks + " Pruefungen bestanden (Altformate, Zaehler, Grenzen, Teilpakete, getrennte Historie/Livewerte).");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
