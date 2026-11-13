using System;
using System.Globalization;
using ModStatistik;

class AccuracyRankingTests
{
    static void Need(bool condition, string message)
    {
        if (!condition) throw new Exception("Accuracy ranking check failed: " + message);
    }

    static void MatchingDisplay(ShotAccuracy accuracy)
    {
        decimal shown = decimal.Parse(accuracy.Percentage.Replace(" %", ""), CultureInfo.GetCultureInfo("de-DE"));
        Need(BloodmoonAccuracyRanking.Value(accuracy) == shown, "ranking equals displayed percentage for " + accuracy.Hits + "/" + accuracy.Shots);
    }

    static int Main()
    {
        try
        {
            var low = new ShotAccuracy(4, 1);
            var best = new ShotAccuracy(4, 3);
            var middle = new ShotAccuracy(4, 2);
            decimal maximum = BloodmoonAccuracyRanking.Maximum(new[] { low, best, middle });
            Need(maximum == 75m && BloodmoonAccuracyRanking.Leads(best, maximum), "single highest result leads");
            Need(!BloodmoonAccuracyRanking.Leads(low, maximum) && !BloodmoonAccuracyRanking.Leads(middle, maximum), "lower results do not lead");

            var equal = new ShotAccuracy(8, 6);
            maximum = BloodmoonAccuracyRanking.Maximum(new[] { low, best, equal });
            Need(BloodmoonAccuracyRanking.Leads(best, maximum) && BloodmoonAccuracyRanking.Leads(equal, maximum), "same ratio at different shot counts ties");
            var third = new ShotAccuracy(12, 9);
            Need(BloodmoonAccuracyRanking.Leads(third, maximum), "all tied players lead, not only two");

            var roundedFirst = new ShotAccuracy(3, 2);
            var roundedSecond = new ShotAccuracy(10000, 6667);
            maximum = BloodmoonAccuracyRanking.Maximum(new[] { roundedFirst, roundedSecond });
            Need(maximum == 66.7m && BloodmoonAccuracyRanking.Leads(roundedFirst, maximum) && BloodmoonAccuracyRanking.Leads(roundedSecond, maximum), "identically displayed rounded percentages tie");
            var lowerRounding = new ShotAccuracy(10000, 6664);
            Need(!BloodmoonAccuracyRanking.Leads(lowerRounding, maximum), "adjacent displayed percentage does not tie");

            var missed = new ShotAccuracy(4, 0);
            var alsoMissed = new ShotAccuracy(10, 0);
            var noShots = new ShotAccuracy(0, 0);
            maximum = BloodmoonAccuracyRanking.Maximum(new[] { missed, alsoMissed, noShots, null });
            Need(maximum == 0m && BloodmoonAccuracyRanking.Leads(missed, maximum) && BloodmoonAccuracyRanking.Leads(alsoMissed, maximum), "all actual zero-percent participants lead");
            Need(!BloodmoonAccuracyRanking.Leads(noShots, maximum) && !BloodmoonAccuracyRanking.Leads(null, maximum), "unknown and zero shots never lead");
            Need(BloodmoonAccuracyRanking.Value(null) == -1m && BloodmoonAccuracyRanking.Value(noShots) == -1m, "missing measurements use sentinel");
            maximum = BloodmoonAccuracyRanking.Maximum(new[] { noShots, null });
            Need(maximum == -1m && !BloodmoonAccuracyRanking.Leads(noShots, maximum) && !BloodmoonAccuracyRanking.Leads(null, maximum), "unknown-only group has no leader");
            Need(BloodmoonAccuracyRanking.Maximum(new ShotAccuracy[0]) == -1m, "empty group has no leader");
            Need(BloodmoonAccuracyRanking.Maximum(null) == -1m, "absent group has no leader");
            Need(!BloodmoonAccuracyRanking.Leads(missed, -1m), "negative maximum cannot designate leader");

            MatchingDisplay(new ShotAccuracy(400, 49));
            Need(BloodmoonAccuracyRanking.Value(new ShotAccuracy(400, 49)) == 12.3m, "rounding midpoint rounds away from zero");
            MatchingDisplay(new ShotAccuracy(2000, 1));
            MatchingDisplay(new ShotAccuracy(2000, 1999));
            MatchingDisplay(roundedFirst);
            MatchingDisplay(roundedSecond);
            MatchingDisplay(new ShotAccuracy(long.MaxValue, long.MaxValue - 1));
            MatchingDisplay(new ShotAccuracy(long.MaxValue, 0));
            for (long shots = 1; shots <= 100; shots++)
                for (long hits = 0; hits <= shots; hits++) MatchingDisplay(new ShotAccuracy(shots, hits));
            Console.WriteLine("PASS: accuracy leaders, exact and rounded ties, zero/unknown values, empty groups and display-consistent rounding.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
