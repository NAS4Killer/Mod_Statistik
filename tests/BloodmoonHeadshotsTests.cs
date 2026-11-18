using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModStatistik;

internal static class BloodmoonHeadshotsTests
{
    private static int checks;
    private static void Need(bool value, string message)
    { checks++; if (!value) throw new Exception(message); }
    private static void Reject(Action action)
    {
        bool rejected = false;
        try { action(); } catch (InvalidDataException) { rejected = true; }
        Need(rejected, "invalid counter accepted");
    }
    private static AccuracyReport Report(string session, long shots, long hits, long? heads)
    { return new AccuracyReport { Day = 7, Session = session, Shots = shots, Hits = hits, Headshots = heads }; }

    private static int Main(string[] args)
    {
        try
        {
            var action = new object(); var details = new object(); bool hit, head;
            var token = BloodmoonShots.Begin(action, details, 1, true);
            BloodmoonShots.Emitted(action, 0);
            BloodmoonShots.Hit(details, 1, true, false);
            Need(BloodmoonShots.WantsHit(details, 1), "body hit must still allow a later head pellet");
            for (int i = 0; i < 12; i++) BloodmoonShots.Hit(details, 1, true, true);
            Need(BloodmoonShots.End(token, true, out hit, out head) && hit && head, "body then head retained once");
            Need(!BloodmoonShots.End(token, true, out hit, out head) && !head, "duplicate finalizer excluded");
            token = BloodmoonShots.Begin(action, details, 1, true);
            BloodmoonShots.Emitted(action, 0);
            BloodmoonShots.Hit(details, 1, false, true);
            Need(BloodmoonShots.End(token, true, out hit, out head) && !hit && !head, "corpse head excluded; reused scope reset");
            token = BloodmoonShots.Begin(action, details, 1, true);
            BloodmoonShots.Emitted(action, 0);
            var inner = BloodmoonShots.Begin(action, details, 1, false);
            BloodmoonShots.Emitted(action, 0); BloodmoonShots.Hit(details, 1, true, true);
            BloodmoonShots.End(inner, true, out hit, out head);
            BloodmoonShots.Hit(details, 1, true, false);
            Need(BloodmoonShots.End(token, true, out hit, out head) && hit && !head, "excluded nested head cannot leak into body hit");

            string session = Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(args[0]);
            var store = new BloodmoonAccuracyStore(args[0], "heads");
            Need(store.Apply("1", "Player", Report(session, 0, 0, 0)), "new zero count");
            Need(store.ForDay(7)["Player"].HeadshotsText == "0", "known zero distinguished from unknown");
            Need(store.Apply("1", "Player", Report(session, 10, 7, 3)), "initial heads");
            Need(!store.Apply("1", "Player", Report(session, 10, 7, 3)), "duplicate counters ignored");
            Need(!store.Apply("1", "Player", Report(session, 11, 8, 2)), "decreasing head count rejected");
            Need(!store.Apply("1", "Player", Report(session, 11, 8, 5)), "head increase cannot exceed hit increase");
            Need(!store.Apply("1", "Player", Report(session, 11, 8, null)), "cannot drop known counters mid-session");
            Reject(delegate { store.Apply("1", "Player", Report(session, 11, 8, 9)); });
            store.Flush(); store = new BloodmoonAccuracyStore(args[0], "heads");
            Need(store.ForDay(7)["Player"].Headshots == 3, "headshots persisted and reloaded");
            Need(store.Apply("1", "Player", Report(Guid.NewGuid().ToString("N"), 3, 2, 1)), "reconnect session accepted");
            Need(store.ForDay(7)["Player"].Headshots == 4, "reconnect sessions summed once");
            store.Apply("1", "Player", Report(Guid.NewGuid().ToString("N"), 2, 1, null));
            Need(!store.ForDay(7)["Player"].Headshots.HasValue, "mixed old and new sessions remain unknown instead of an incomplete total");
            store.Flush(); store = new BloodmoonAccuracyStore(args[0], "heads");
            Need(!store.ForDay(7)["Player"].Headshots.HasValue, "mixed availability persists");
            File.WriteAllText(Path.Combine(args[0], "bloodmoon_accuracy-old.json"),
                "{\"Version\":1,\"Nights\":[{\"Day\":7,\"Players\":[{\"Id\":\"1\",\"Name\":\"Player\",\"Sessions\":[{\"Id\":\"" + session + "\",\"Shots\":10,\"Hits\":7}]}]}]}");
            var old = new BloodmoonAccuracyStore(args[0], "old").ForDay(7)["Player"];
            Need(old.Percentage == "70 %" && old.HeadshotsText == "\u2014", "0.3.1 history loads without invented headshots");

            var log = BloodmoonLog.FromNights(new[] { new BloodmoonNight(7,
                new Dictionary<string, long> { { "Player", 4 } },
                new Dictionary<string, ShotAccuracy> { { "Player", new ShotAccuracy(10, 7, 3) } }) });
            byte[] bytes = StatisticsWire.EncodeAccuracy(log, 7, true);
            Need(bytes[0] == 4 && StatisticsWire.Decode(bytes).Nights[0].Accuracy["Player"].Headshots == 3, "new payload roundtrip");
            Need(!StatisticsWire.Decode(StatisticsWire.EncodeAccuracy(log, 7)).Nights[0].Accuracy["Player"].Headshots.HasValue, "old payload unchanged");
            byte[] invalid = (byte[])bytes.Clone();
            Buffer.BlockCopy(BitConverter.GetBytes(8L), 0, invalid, invalid.Length - 8, 8);
            Reject(delegate { StatisticsWire.Decode(invalid); });
            Buffer.BlockCopy(BitConverter.GetBytes(-2L), 0, invalid, invalid.Length - 8, 8);
            Reject(delegate { StatisticsWire.Decode(invalid); });
            for (int length = 0; length < bytes.Length; length++)
            {
                bool failed = false;
                try { StatisticsWire.Decode(bytes.Take(length).ToArray()); }
                catch (InvalidDataException) { failed = true; }
                catch (IOException) { failed = true; }
                Need(failed, "truncated headshot payload accepted");
            }
            var report = Report(session, 10, 7, 3); report.Final = true;
            var frame = StatisticsMessage.Decode(StatisticsMessage.RequestExtended(1, null, report, true));
            Need(frame.Headshots && frame.Report.Headshots == 3 && frame.Report.Final, "headshot report roundtrip");
            frame = StatisticsMessage.Decode(StatisticsMessage.RequestExtended(1, null, report));
            Need(!frame.Headshots && !frame.Report.Headshots.HasValue && frame.Report.Hits == 7, "old request remains compatible");
            report.Headshots = -1;
            Reject(delegate { StatisticsMessage.RequestExtended(1, null, report, true); });
            var history = StatisticsWire.EncodeAccuracy(BloodmoonLog.FromNights(new BloodmoonNight[0]), 0, true);
            var delta = StatisticsDelta.Decode(StatisticsDelta.Encode(history, StatisticsWire.Hash(history), bytes, true));
            Need(delta.HistoryPayload[0] == 4 && StatisticsWire.Decode(delta.LivePayload).Nights[0].Accuracy["Player"].Headshots == 3, "headshots in delta envelope");
            Console.WriteLine("PASS: " + checks + " headshot checks: pellets, nested scopes, storage, migration, reconnects, invalid counters and wire compatibility.");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
