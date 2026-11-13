using System;
using System.Collections.Generic;
using ModStatistik;

class HudHoldTests
{
    static void Need(bool condition, string message)
    {
        if (!condition) throw new Exception("HUD hold check failed: " + message);
    }

    static int Main()
    {
        try
        {
            var previous = new ShotAccuracy(8, 6);
            var done = new BloodmoonNight(7, new Dictionary<string, long> { { "Player", 24 } },
                new Dictionary<string, ShotAccuracy> { { "Player", previous } });
            long kills;
            Need(BloodmoonHudDisplay.TryValue(null, done, 100, 100, "Player", out kills) && kills == 24, "finished kills visible immediately");
            Need(BloodmoonHudDisplay.TryValue(null, done, 100, 159.99f, "Player", out kills) && kills == 24, "finished kills visible before expiry");
            Need(!BloodmoonHudDisplay.TryValue(null, done, 100, 160, "Player", out kills), "finished kills expire at 60 seconds");
            Need(!BloodmoonHudDisplay.TryValue(null, done, 100, 99, "Player", out kills), "finished kills hidden before finish time");
            Need(!BloodmoonHudDisplay.TryValue(null, null, 100, 101, "Player", out kills), "missing finished kills");
            Need(!BloodmoonHudDisplay.TryValue(null, done, 100, 101, "Other", out kills), "missing player kills");

            ShotAccuracy accuracy;
            Need(BloodmoonHudDisplay.TryAccuracy(null, done, 100, 100, "Player", out accuracy) && accuracy == previous, "finished accuracy visible immediately");
            Need(BloodmoonHudDisplay.TryAccuracy(null, done, 100, 159.99f, "Player", out accuracy) && accuracy == previous, "finished accuracy visible before expiry");
            Need(!BloodmoonHudDisplay.TryAccuracy(null, done, 100, 160, "Player", out accuracy) && accuracy == null, "finished accuracy expires at 60 seconds");
            Need(!BloodmoonHudDisplay.TryAccuracy(null, done, 100, 99, "Player", out accuracy) && accuracy == null, "finished accuracy hidden before finish time");
            Need(!BloodmoonHudDisplay.TryAccuracy(null, null, 100, 101, "Player", out accuracy), "missing finished accuracy");
            Need(!BloodmoonHudDisplay.TryAccuracy(null, done, 100, 101, "Other", out accuracy), "missing player accuracy");
            Need(!BloodmoonHudDisplay.TryAccuracy(null, done, 100, 101, null, out accuracy), "null player name");
            Need(!BloodmoonHudDisplay.TryAccuracy(null, done, 100, 101, "", out accuracy), "empty player name");

            var current = new ShotAccuracy(4, 2);
            var live = new BloodmoonNight(14, new Dictionary<string, long> { { "Player", 3 } },
                new Dictionary<string, ShotAccuracy> { { "Player", current } });
            Need(BloodmoonHudDisplay.TryValue(live, done, 100, 170, "Player", out kills) && kills == 3, "new night replaces finished kills");
            Need(BloodmoonHudDisplay.TryAccuracy(live, done, 100, 170, "Player", out accuracy) && accuracy == current, "live accuracy survives old hold expiry");
            Need(BloodmoonHudDisplay.TryAccuracy(live, done, 100, 101, "Player", out accuracy) && accuracy == current, "new night replaces held accuracy immediately");

            var unknownLive = new BloodmoonNight(21, new Dictionary<string, long> { { "Player", 0 } });
            Need(!BloodmoonHudDisplay.TryAccuracy(unknownLive, done, 100, 101, "Player", out accuracy) && accuracy == null, "new night never inherits previous accuracy");
            var noShots = new ShotAccuracy(0, 0);
            var zeroLive = new BloodmoonNight(21, new Dictionary<string, long> { { "Player", 0 } },
                new Dictionary<string, ShotAccuracy> { { "Player", noShots } });
            Need(BloodmoonHudDisplay.TryAccuracy(zeroLive, done, 100, 101, "Player", out accuracy) && accuracy == noShots, "zero shots retained as known empty measurement");
            Need(!BloodmoonHudDisplay.TryAccuracy(null, unknownLive, 100, 101, "Player", out accuracy), "legacy history accuracy absent");
            Console.WriteLine("PASS: HUD kills and accuracy, 60-second boundary, reset, missing values and new-night precedence.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
