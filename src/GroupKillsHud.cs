using System;
using System.Globalization;
using System.Linq;
using ModStatistik;
using UnityEngine;

namespace ModStatistik
{
    internal static class GroupAccuracyRanking
    {
        internal static decimal Maximum(EntityPlayer local)
        {
            decimal maximum = -1m;
            ShotAccuracy accuracy;
            if (local == null) return maximum;
            if (StatisticsSync.TryHudAccuracy(local.EntityName, out accuracy))
                maximum = BloodmoonAccuracyRanking.Value(accuracy);
            if (local.Party != null)
                foreach (var member in local.Party.MemberList)
                    if (member != null && StatisticsSync.TryHudAccuracy(member.EntityName, out accuracy))
                        maximum = Math.Max(maximum, BloodmoonAccuracyRanking.Value(accuracy));
            return maximum;
        }
    }

    internal static class GroupKillRanking
    {
        internal static bool Leads(long value, long maximum) { return maximum > 0 && value == maximum; }
        internal static long Maximum(EntityPlayer local)
        {
            long maximum = 0, value;
            if (local == null) return maximum;
            if (StatisticsSync.TryHudValue(local.EntityName, out value)) maximum = value;
            if (local.Party != null)
                foreach (var member in local.Party.MemberList)
                    if (member != null && StatisticsSync.TryHudValue(member.EntityName, out value)) maximum = Math.Max(maximum, value);
            return maximum;
        }
        internal static bool InGroup(EntityPlayer local) { return local != null && local.Party != null && local.Party.MemberList.Count > 1; }
    }
}
