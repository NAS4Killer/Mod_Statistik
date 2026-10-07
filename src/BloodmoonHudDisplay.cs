namespace ModStatistik
{
    internal static class BloodmoonHudDisplay
    {
        internal const float HoldSeconds = 60f;
        internal static bool TryValue(BloodmoonNight live, BloodmoonNight finished, float finishedAt, float now, string name, out long value)
        {
            value = 0;
            if (string.IsNullOrEmpty(name)) return false;
            if (live != null) return live.Kills.TryGetValue(name, out value);
            return finished != null && now >= finishedAt && now - finishedAt < HoldSeconds && finished.Kills.TryGetValue(name, out value);
        }
        internal static long Value(BloodmoonNight live, BloodmoonNight finished, string name)
        { return (live ?? finished).Kills[name]; }
    }
}
