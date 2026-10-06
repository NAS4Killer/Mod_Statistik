using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ModStatistik
{
    internal static class StatisticsWire
    {
        internal const int MaximumBytes = 256 * 1024;
        internal const int ChunkBytes = 8192;
        internal const int MaximumNights = 512;
        internal const int MaximumPlayers = 128;
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        internal static byte[] Encode(BloodmoonLog data)
        {
            if (data == null || data.Nights.Count > MaximumNights || data.Players.Count > MaximumPlayers)
                throw new InvalidDataException("Statistik zu gross.");
            using (var memory = new MemoryStream())
            using (var writer = new BinaryWriter(memory, Utf8))
            {
                writer.Write((byte)1);
                writer.Write((ushort)data.Nights.Count);
                foreach (var night in data.Nights)
                {
                    writer.Write(night.Day);
                    writer.Write((ushort)night.Kills.Count);
                    foreach (var record in night.Kills.OrderBy(k => k.Key, StringComparer.Ordinal))
                    {
                        byte[] name = Utf8.GetBytes(record.Key);
                        ValidateName(record.Key, name.Length);
                        if (record.Value < 0) throw new InvalidDataException("Negative Kills.");
                        writer.Write((byte)name.Length);
                        writer.Write(name);
                        writer.Write(record.Value);
                    }
                    if (memory.Length > MaximumBytes) throw new InvalidDataException("Statistik zu gross.");
                }
                return memory.ToArray();
            }
        }

        internal static BloodmoonLog Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 3 || bytes.Length > MaximumBytes)
                throw new InvalidDataException("Ungueltige Statistikgroesse.");
            using (var memory = new MemoryStream(bytes, false))
            using (var reader = new BinaryReader(memory, Utf8))
            {
                if (reader.ReadByte() != 1) throw new InvalidDataException("Unbekanntes Statistikformat.");
                int count = reader.ReadUInt16();
                if (count > MaximumNights) throw new InvalidDataException("Zu viele Naechte.");
                var nights = new List<BloodmoonNight>();
                var days = new HashSet<int>();
                var players = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < count; i++)
                {
                    int day = reader.ReadInt32();
                    int entries = reader.ReadUInt16();
                    if (day <= 0 || !days.Add(day) || entries > MaximumPlayers)
                        throw new InvalidDataException("Ungueltiger Blutmondtag.");
                    var kills = new Dictionary<string, long>(StringComparer.Ordinal);
                    for (int j = 0; j < entries; j++)
                    {
                        int length = reader.ReadByte();
                        if (length == 0 || length > 64) throw new InvalidDataException("Ungueltiger Name.");
                        byte[] nameBytes = reader.ReadBytes(length);
                        if (nameBytes.Length != length) throw new EndOfStreamException();
                        string name = Utf8.GetString(nameBytes);
                        ValidateName(name, length);
                        long value = reader.ReadInt64();
                        if (value < 0 || kills.ContainsKey(name)) throw new InvalidDataException("Ungueltiger Killwert.");
                        kills.Add(name, value);
                        players.Add(name);
                        if (players.Count > MaximumPlayers) throw new InvalidDataException("Zu viele Spieler.");
                    }
                    var night = new BloodmoonNight(day, kills);
                    long total = night.Total;
                    nights.Add(night);
                }
                if (memory.Position != memory.Length) throw new InvalidDataException("Zusaetzliche Statistikdaten.");
                return BloodmoonLog.FromNights(nights);
            }
        }

        private static void ValidateName(string name, int bytes)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 16 || bytes > 64 || name.Any(char.IsControl))
                throw new InvalidDataException("Ungueltiger Spielername.");
        }

        internal static byte[] Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return sha.ComputeHash(bytes);
        }

        internal static bool Equal(byte[] left, byte[] right)
        {
            return left != null && right != null && left.SequenceEqual(right);
        }
    }

    internal sealed class StatisticsTransfer
    {
        private int requestId, total, received;
        private byte[] hash;
        private byte[][] chunks;

        internal void Reset() { requestId = 0; chunks = null; received = 0; hash = null; }

        internal byte[] Accept(int request, int index, int count, int length, byte[] digest, byte[] data)
        {
            int expectedCount = (length + StatisticsWire.ChunkBytes - 1) / StatisticsWire.ChunkBytes;
            if (request <= 0 || length <= 0 || length > StatisticsWire.MaximumBytes || count != expectedCount ||
                index < 0 || index >= count || digest == null || digest.Length != 32 || data == null ||
                data.Length != Math.Min(StatisticsWire.ChunkBytes, length - index * StatisticsWire.ChunkBytes))
                throw new InvalidDataException("Ungueltiges Statistikpaket.");
            if (chunks == null || requestId != request)
            {
                requestId = request; total = length; hash = (byte[])digest.Clone();
                chunks = new byte[count][]; received = 0;
            }
            if (total != length || chunks.Length != count || !StatisticsWire.Equal(hash, digest))
                throw new InvalidDataException("Widerspruechliche Statistikpakete.");
            if (chunks[index] != null)
            {
                if (!StatisticsWire.Equal(chunks[index], data)) throw new InvalidDataException("Widerspruechliches Teilpaket.");
                return null;
            }
            chunks[index] = (byte[])data.Clone(); received++;
            if (received != chunks.Length) return null;
            byte[] result = new byte[total];
            for (int i = 0; i < chunks.Length; i++) Buffer.BlockCopy(chunks[i], 0, result, i * StatisticsWire.ChunkBytes, chunks[i].Length);
            if (!StatisticsWire.Equal(StatisticsWire.Hash(result), hash)) throw new InvalidDataException("Statistikpruefsumme stimmt nicht.");
            Reset();
            return result;
        }
    }
}
