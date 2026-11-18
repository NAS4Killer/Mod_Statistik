using System;
using System.IO;

namespace ModStatistik
{
    // A live response carries the small current night on every update. Completed
    // nights travel only when the client does not already have this history hash.
    internal sealed class StatisticsDelta
    {
        internal byte[] HistoryHash, HistoryPayload, LivePayload;

        internal static byte[] Encode(byte[] historyPayload, byte[] historyHash, byte[] livePayload, bool includeHistory)
        {
            if (historyHash == null || historyHash.Length != 32) throw new InvalidDataException("Ungueltige Historienpruefsumme.");
            ValidateLive(livePayload);
            if (includeHistory) ValidateHistory(historyPayload, historyHash);
            int historyLength = includeHistory ? historyPayload.Length : 0;
            long total = 1L + 32 + 4 + historyLength + 4 + livePayload.Length;
            if (total > StatisticsWire.MaximumBytes) throw new InvalidDataException("Statistik zu gross.");
            using (var stream = new MemoryStream((int)total))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((byte)1); writer.Write(historyHash);
                writer.Write(historyLength);
                if (includeHistory) writer.Write(historyPayload);
                writer.Write(livePayload.Length); writer.Write(livePayload);
                return stream.ToArray();
            }
        }

        internal static StatisticsDelta Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 42 || bytes.Length > StatisticsWire.MaximumBytes)
                throw new InvalidDataException("Ungueltige Statistikgroesse.");
            using (var stream = new MemoryStream(bytes, false))
            using (var reader = new BinaryReader(stream))
            {
                if (reader.ReadByte() != 1) throw new InvalidDataException("Unbekanntes Aktualisierungsformat.");
                var delta = new StatisticsDelta { HistoryHash = reader.ReadBytes(32) };
                if (delta.HistoryHash.Length != 32) throw new EndOfStreamException();
                int historyLength = reader.ReadInt32();
                // Reserve the following length field and at least one live byte
                // before allocating, including when a malicious length overflows.
                if (historyLength < 0 || historyLength > stream.Length - stream.Position - 5)
                    throw new InvalidDataException("Ungueltige Historiengroesse.");
                if (historyLength > 0)
                {
                    delta.HistoryPayload = reader.ReadBytes(historyLength);
                    ValidateHistory(delta.HistoryPayload, delta.HistoryHash);
                }
                int liveLength = reader.ReadInt32();
                if (liveLength <= 0 || liveLength != stream.Length - stream.Position)
                    throw new InvalidDataException("Ungueltige Livegroesse.");
                delta.LivePayload = reader.ReadBytes(liveLength);
                ValidateLive(delta.LivePayload);
                return delta;
            }
        }

        private static void ValidateHistory(byte[] bytes, byte[] hash)
        {
            if (bytes == null || bytes.Length == 0 || bytes.Length > StatisticsWire.MaximumBytes || (bytes[0] != 3 && bytes[0] != 4))
                throw new InvalidDataException("Ungueltiges Historienformat.");
            if (!StatisticsWire.Equal(StatisticsWire.Hash(bytes), hash))
                throw new InvalidDataException("Historienpruefsumme stimmt nicht.");
            int activeDay;
            StatisticsWire.Decode(bytes, out activeDay);
            if (activeDay != 0) throw new InvalidDataException("Aktive Nacht in Historie.");
        }

        private static void ValidateLive(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0 || bytes.Length > StatisticsWire.MaximumBytes || (bytes[0] != 3 && bytes[0] != 4))
                throw new InvalidDataException("Ungueltiges Liveformat.");
            int activeDay;
            BloodmoonLog live = StatisticsWire.Decode(bytes, out activeDay);
            if (activeDay == 0 ? live.Nights.Count != 0 : live.Nights.Count != 1)
                throw new InvalidDataException("Ungueltige Liveauswahl.");
        }
    }
}
