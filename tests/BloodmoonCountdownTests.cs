using System;using ModStatistik;
class CountdownTests {static void Need(bool ok){if(!ok)throw new Exception("Countdown test failed");}static int Main(){try{
double fallback=BloodmoonCountdown.ResolveSpeed(0,60);Need(Math.Abs(fallback-24000.0/3600)<0.000001);
Need(BloodmoonCountdown.ResolveSpeed(double.NaN,120)==24000.0/7200);
Need(BloodmoonCountdown.ResolveSpeed(double.PositiveInfinity,60)==fallback);
Need(BloodmoonCountdown.ResolveSpeed(12,60)==12);
Need(BloodmoonCountdown.ResolveSpeed(0,0)==0);
string server=BloodmoonCountdown.Format(6UL*24000+21000,7,22,4,fallback);Need(server.Contains("0 Std. 2 Min. 30 Sek.")&&!server.Contains("unbekannt"));
string s=BloodmoonCountdown.Format(6UL*24000+21000,7,22,4,24000.0/3600);Need(s.Contains("Tag 7, 22:00")&&s.Contains("0 Spieltage 1 Std. 0 Min.")&&s.Contains("0 Std. 2 Min. 30 Sek."));
s=BloodmoonCountdown.Format(6UL*24000+22000,7,22,4,6);Need(s.Contains("Blutmond läuft"));
s=BloodmoonCountdown.Format(7UL*24000+3000,7,22,4,6);Need(s.Contains("Blutmond läuft"));
s=BloodmoonCountdown.Format(7UL*24000+4000,7,22,4,6);Need(s.Contains("noch nicht bekannt"));
s=BloodmoonCountdown.Format(6UL*24000+10000,7,18,6,0);Need(s.Contains("18:00")&&s.Contains("Realzeit ca. unbekannt"));
s=BloodmoonCountdown.Format(6UL*24000,0,22,4,6);Need(s.Contains("noch nicht bekannt"));
s=BloodmoonCountdown.Format(0,91,22,4,24000.0/3600);Need(s.Contains("90 Spieltage 22 Std."));
Console.WriteLine("Blutmondtag, Ingame-Restzeit, Realzeit, angepasste Nachtzeiten, laufender Blutmond und unbekannter Termin bestanden.");return 0;}catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}}}
