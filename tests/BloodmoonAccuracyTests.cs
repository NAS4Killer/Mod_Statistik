using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ModStatistik;

internal static class BloodmoonAccuracyTests
{
    private const string SessionA = "11111111111111111111111111111111";
    private const string SessionB = "22222222222222222222222222222222";
    private static void Need(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Reject(Action action, string message)
    {
        try { action(); }
        catch (InvalidDataException) { return; }
        catch (ArgumentException) { return; }
        throw new Exception(message);
    }
    private static AccuracyReport Report(long shots, long hits, string session = SessionA, int day = 7)
    { return new AccuracyReport { Day = day, Session = session, Shots = shots, Hits = hits }; }
    private static string FileName(string directory) { return Path.Combine(directory, "bloodmoon_accuracy-world.json"); }
    private static string NewDirectory(string root)
    {
        string result = Path.Combine(root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(result); return result;
    }

    private static void Main(string[] args)
    {
        string root = NewDirectory(args[0]);
        Need(new ShotAccuracy(0, 0).Percentage == "\u2014", "No shots have no percentage.");
        Need(new ShotAccuracy(5, 0).Percentage == "0 %", "Misses are a known zero percent.");
        Need(new ShotAccuracy(4, 3).Percentage == "75 %", "Exact percentage.");
        Need(new ShotAccuracy(3, 1).Percentage == "33,3 %", "German decimal and one digit.");
        Need(new ShotAccuracy(long.MaxValue, long.MaxValue).Percentage == "100 %", "Percentage does not overflow.");
        Reject(() => new ShotAccuracy(-1, 0), "Negative shots accepted.");
        Reject(() => new ShotAccuracy(1, 2), "Too many successful shots accepted.");

        string directory = NewDirectory(root);
        string killPath = Path.Combine(directory, "bloodmoon_kills.txt");
        byte[] legacy = new UTF8Encoding(false).GetBytes("=== Blutmond Tag 7 END test ===\nPlayer start=1 end=3 bm_kills=2\n");
        File.WriteAllBytes(killPath, legacy);
        var store = new BloodmoonAccuracyStore(directory, "world");
        Need(store.Revision == 0 && store.ForDay(7).Count == 0, "Empty store.");
        store.Flush();
        Need(!File.Exists(FileName(directory)), "Unchanged store must not write.");
        Need(store.Apply("id-a", "Player", Report(0, 0)), "Known zero-shot player added.");
        Need(store.ForDay(7)["Player"].Percentage == "\u2014", "Zero-shot record retained.");
        Need(!File.Exists(FileName(directory)), "Apply must only update memory.");
        Need(store.Apply("id-a", "Player", Report(10, 6)), "Cumulative totals added.");
        long revision = store.Revision;
        Need(!store.Apply("id-a", "Player", Report(10, 6)) && store.Revision == revision, "Duplicate report.");
        Need(!store.Apply("id-a", "Player", Report(9, 5)) && store.Revision == revision, "Older report.");
        Need(!store.Apply("id-a", "Player", Report(11, 5)) && store.Revision == revision, "Regressing hit count.");
        Need(!store.Apply("id-a", "Player", Report(10, 7)) && store.Revision == revision,
            "Completed firearm shots cannot gain an extra hit without a new shot.");
        Need(!store.Apply("id-a", "Player", Report(11, 8)) && store.Revision == revision,
            "Successful-shot increment cannot exceed the emitted-shot increment.");
        Need(store.Apply("id-a", "Player", Report(4, 3, SessionB)), "New reconnect session.");
        Need(store.ForDay(7)["Player"].Shots == 14 && store.ForDay(7)["Player"].Hits == 9, "Reconnect totals combine once.");
        revision = store.Revision;
        Need(!store.Apply("id-a", "Player", Report(4, 3, SessionB)) && store.Revision == revision, "Reconnect duplicate.");
        Need(store.Apply("id-a", "Renamed", Report(4, 3, SessionB)), "Current name updated.");
        Need(store.ForDay(7).Count == 1 && !store.ForDay(7).ContainsKey("Player") && store.ForDay(7)["Renamed"].Shots == 14, "Rename preserves stable identity.");
        var snapshot = store.ForDay(7); snapshot.Clear();
        Need(store.ForDay(7).Count == 1, "Returned dictionary cannot alter store.");
        Need(store.Apply("id-b", "Other", Report(0, 0)), "Another zero-shot player.");
        Need(store.Apply("id-a", "Renamed", Report(2, 2, SessionA, 14)), "Same session may start a new night.");
        store.Flush();
        Need(File.Exists(FileName(directory)), "Flush writes sidecar.");
        Need(File.ReadAllBytes(killPath).SequenceEqual(legacy), "Original kill history untouched.");
        byte[] saved = File.ReadAllBytes(FileName(directory));
        store.Flush();
        Need(File.ReadAllBytes(FileName(directory)).SequenceEqual(saved), "Clean flush is unchanged.");
        store = new BloodmoonAccuracyStore(directory, "world");
        Need(store.ForDay(7)["Renamed"].Shots == 14 && store.ForDay(7)["Renamed"].Hits == 9 && store.ForDay(14)["Renamed"].Shots == 2, "Restart restores nights and sessions.");
        Need(!store.Apply("id-a", "Renamed", Report(4, 3, SessionB)), "Restart duplicate not counted again.");
        Need(store.Apply("id-a", "Renamed", Report(6, 4, SessionB)), "Restart cumulative increment.");
        Need(store.ForDay(7)["Renamed"].Shots == 16 && store.ForDay(7)["Renamed"].Hits == 10, "Only increment added.");
        revision = store.Revision;
        Reject(() => store.Apply("id-a", "Renamed", Report(3, 4)), "Hits exceed shots.");
        Reject(() => store.Apply("id-a", "Renamed", Report(-1, 0)), "Negative report.");
        Reject(() => store.Apply("id-a", "Renamed", Report(1, 0, "bad")), "Malformed session.");
        Reject(() => store.Apply("id-a", "Renamed", Report(1, 0, new string('0', 32))), "Empty GUID.");
        Reject(() => store.Apply("id-a", "Renamed", Report(1, 0, SessionA, 0)), "Invalid night.");
        Reject(() => store.Apply("id-c", "Renamed", Report(1, 0)), "Duplicate displayed identity.");
        Reject(() => store.Apply("id-c", "TooLongPlayerNameX", Report(1, 0)), "Overlong player name.");
        Reject(() => store.Apply("id-c", "Player\n", Report(1, 0)), "Control character.");
        Reject(() => store.Apply("id-a", "Renamed", Report(BloodmoonAccuracyStore.MaximumShots + 1, 0)), "Oversized counter.");
        Need(store.Revision == revision && store.ForDay(7)["Renamed"].Shots == 16, "Invalid reports do not mutate data.");
        Reject(() => new BloodmoonAccuracyStore(directory, "../escape"), "World key path traversal.");

        var caseStore = new BloodmoonAccuracyStore(NewDirectory(root), "world");
        string mixed = "abcdefabcdefabcdefabcdefabcdefab";
        caseStore.Apply("p", "P", Report(3, 2, mixed));
        Need(!caseStore.Apply("p", "P", Report(3, 2, mixed.ToUpperInvariant())), "GUID case cannot double count.");
        var limitStore = new BloodmoonAccuracyStore(NewDirectory(root), "world");
        for (int i = 0; i < BloodmoonAccuracyStore.MaximumSessionsPerPlayer; i++)
            limitStore.Apply("p", "P", Report(1, 1, Guid.NewGuid().ToString("N")));
        Reject(() => limitStore.Apply("p", "P", Report(1, 1, Guid.NewGuid().ToString("N"))), "Session limit.");
        Need(limitStore.ForDay(7)["P"].Shots == BloodmoonAccuracyStore.MaximumSessionsPerPlayer, "Rejected session leaves totals intact.");

        string broken = NewDirectory(root);
        byte[] corrupt = Encoding.UTF8.GetBytes("broken original");
        File.WriteAllBytes(FileName(broken), corrupt);
        Reject(() => new BloodmoonAccuracyStore(broken, "world"), "Corrupt file accepted.");
        Need(File.ReadAllBytes(FileName(broken)).SequenceEqual(corrupt), "Corrupt original preserved.");
        string invalid = NewDirectory(root);
        byte[] invalidSaved = Encoding.UTF8.GetBytes("{\"Version\":1,\"Nights\":[{\"Day\":7,\"Players\":[{\"Id\":\"p\",\"Name\":\"P\",\"Sessions\":[{\"Id\":\"" + SessionA + "\",\"Shots\":1,\"Hits\":2}]}]}]}");
        File.WriteAllBytes(FileName(invalid), invalidSaved);
        Reject(() => new BloodmoonAccuracyStore(invalid, "world"), "Invalid saved counts accepted.");
        Need(File.ReadAllBytes(FileName(invalid)).SequenceEqual(invalidSaved), "Invalid saved counts preserved.");

        var old = BloodmoonLog.Parse(Encoding.UTF8.GetString(legacy));
        Need(old.Nights.Single().Accuracy.Count == 0 && old.Nights.Single().Kills["Player"] == 2, "Old TXT has unknown accuracy and unchanged kills.");
        var supplied = new Dictionary<string, ShotAccuracy> { { "Player", new ShotAccuracy(4, 3) } };
        var night = new BloodmoonNight(7, new Dictionary<string, long> { { "Player", 2 } }, supplied);
        supplied.Clear();
        Need(night.Accuracy["Player"].Percentage == "75 %", "Night owns its accuracy snapshot.");
        Console.WriteLine("PASS: accuracy percentages, cumulative sessions, duplicates, reorder, reconnect, rename, restart, bounds, corruption preservation and unchanged kill TXT.");
    }
}
