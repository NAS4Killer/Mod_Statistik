using System;
using System.Collections.Generic;

namespace ModStatistik
{
    internal static class BloodmoonAccuracyRanking
    {
        // Rank the displayed percentage so equally displayed results share the lead.
        internal static decimal Value(ShotAccuracy accuracy)
        {
            return accuracy == null || accuracy.Shots == 0
                ? -1m
                : decimal.Round(100m * accuracy.Hits / accuracy.Shots, 1, MidpointRounding.AwayFromZero);
        }

        internal static bool Leads(ShotAccuracy accuracy, decimal maximum)
        {
            return maximum >= 0m && Value(accuracy) == maximum;
        }

        internal static decimal Maximum(IEnumerable<ShotAccuracy> values)
        {
            decimal maximum = -1m;
            if (values != null)
                foreach (var accuracy in values)
                    maximum = Math.Max(maximum, Value(accuracy));
            return maximum;
        }
    }
}
