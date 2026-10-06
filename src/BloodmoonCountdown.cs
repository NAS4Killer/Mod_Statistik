using System;
using System.Globalization;
namespace ModStatistik
{
    internal static class BloodmoonCountdown
    {
        internal static double ResolveSpeed(double runtimeRate, int dayLengthMinutes)
        {
            if (runtimeRate > 0 && !double.IsNaN(runtimeRate) && !double.IsInfinity(runtimeRate)) return runtimeRate;
            return dayLengthMinutes > 0 ? 24000.0 / (dayLengthMinutes * 60.0) : 0;
        }

        internal static string Format(ulong now, int day, int dusk, int dawn, double ticksPerSecond)
        {
            if (day <= 0) return "Nächster Blutmond: noch nicht bekannt";
            ulong start = (ulong)(day - 1) * 24000UL + (ulong)dusk * 1000UL;
            ulong end = (ulong)day * 24000UL + (ulong)dawn * 1000UL;
            if (now >= start && now < end) return "Blutmond läuft · Tag " + day;
            if (now >= end) return "Nächster Blutmond: noch nicht bekannt";
            ulong remaining = start - now;
            int days = (int)(remaining / 24000UL);
            int hours = (int)((remaining % 24000UL) / 1000UL);
            int minutes = (int)((remaining % 1000UL) * 60UL / 1000UL);
            string real = "unbekannt";
            if (ticksPerSecond > 0 && !double.IsNaN(ticksPerSecond) && !double.IsInfinity(ticksPerSecond))
            {
                double seconds = Math.Ceiling(remaining / ticksPerSecond);
                if (seconds <= TimeSpan.MaxValue.TotalSeconds)
                {
                    var span = TimeSpan.FromSeconds(seconds);
                    real = ((long)span.TotalHours).ToString(CultureInfo.InvariantCulture) + " Std. " + span.Minutes + " Min. " + span.Seconds + " Sek.";
                }
            }
            return "Nächster Blutmond: Tag " + day + ", " + dusk.ToString("00") + ":00 · Noch " + days + " Spieltage " + hours + " Std. " + minutes + " Min. · Realzeit ca. " + real;
        }
    }
}
