using System;
using System.IO;

namespace ModStatistik
{
    internal sealed class StatisticsMessage
    {
        internal const string Prefix = "[ModStatistik:1]";
        internal const int MaximumTextLength = 12000;
        internal bool IsRequest;
        internal bool Extended;
        internal bool Headshots;
        internal AccuracyReport Report;
        internal int Id, Index, Count, Total;
        internal byte Result;
        internal byte[] Hash, Chunk;

        internal static bool IsReserved(string message)
        { return message != null && message.StartsWith(Prefix, StringComparison.Ordinal); }

        internal static string Request(int id, byte[] hash)
        { return WriteRequest(id, hash, null, false); }

        internal static string RequestExtended(int id, byte[] historyHash, AccuracyReport report, bool headshots = false)
        { return WriteRequest(id, historyHash, report, true, headshots); }

        private static string WriteRequest(int id, byte[] hash, AccuracyReport report, bool extended, bool headshots = false)
        {
            if (id <= 0 || (hash != null && hash.Length != 32)) throw new InvalidDataException();
            if (report != null) ValidateReport(report);
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((byte)(extended ? (headshots ? 5 : 3) : 1)); writer.Write(id); writer.Write(hash ?? new byte[32]);
                if (extended)
                {
                    writer.Write((byte)(report == null ? 0 : 1));
                    if (report != null)
                    {
                        writer.Write(report.Day); writer.Write(new Guid(report.Session).ToByteArray());
                        writer.Write(report.Shots); writer.Write(report.Hits);
                        if (headshots) writer.Write(report.Headshots ?? -1L);
                        writer.Write((byte)(report.Final ? 1 : 0));
                    }
                }
                return Prefix + Convert.ToBase64String(stream.ToArray());
            }
        }

        internal static string Reply(int id, byte result, int index, int count, int total,
            byte[] hash, byte[] chunk)
        { return WriteReply(id, result, index, count, total, hash, chunk, false); }

        internal static string ReplyExtended(int id, byte result, int index, int count, int total,
            byte[] hash, byte[] chunk, bool headshots = false)
        { return WriteReply(id, result, index, count, total, hash, chunk, true, headshots); }

        private static string WriteReply(int id, byte result, int index, int count, int total,
            byte[] hash, byte[] chunk, bool extended, bool headshots = false)
        {
            var frame = new StatisticsMessage { Id = id, Result = result, Index = index,
                Count = count, Total = total, Hash = hash ?? new byte[32], Chunk = chunk };
            ValidateReply(frame);
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((byte)(extended ? (headshots ? 6 : 4) : 2)); writer.Write(id); writer.Write(result);
                writer.Write((ushort)index); writer.Write((ushort)count); writer.Write(total);
                writer.Write(frame.Hash); writer.Write((ushort)chunk.Length); writer.Write(chunk);
                return Prefix + Convert.ToBase64String(stream.ToArray());
            }
        }

        internal static StatisticsMessage Decode(string message)
        {
            if (!IsReserved(message) || message.Length > MaximumTextLength)
                throw new InvalidDataException();
            byte[] bytes = Convert.FromBase64String(message.Substring(Prefix.Length));
            using (var stream = new MemoryStream(bytes, false))
            using (var reader = new BinaryReader(stream))
            {
                byte kind = reader.ReadByte();
                var frame = new StatisticsMessage { Id = reader.ReadInt32() };
                if (frame.Id <= 0) throw new InvalidDataException();
                frame.Headshots = kind == 5 || kind == 6;
                frame.Extended = kind == 3 || kind == 4 || frame.Headshots;
                if (kind == 1 || kind == 3 || kind == 5)
                {
                    frame.IsRequest = true; frame.Hash = reader.ReadBytes(32);
                    if (frame.Hash.Length != 32) throw new InvalidDataException();
                    if (frame.Extended)
                    {
                        byte hasReport = reader.ReadByte();
                        if (hasReport > 1) throw new InvalidDataException();
                        if (hasReport == 1)
                        {
                            int day = reader.ReadInt32();
                            byte[] session = reader.ReadBytes(16);
                            if (session.Length != 16) throw new EndOfStreamException();
                            frame.Report = new AccuracyReport { Day = day, Session = new Guid(session).ToString("N"),
                                Shots = reader.ReadInt64(), Hits = reader.ReadInt64() };
                            if (frame.Headshots) frame.Report.Headshots = StatisticsWire.ReadHeadshots(reader, frame.Report.Hits);
                            byte final = reader.ReadByte();
                            if (final > 1) throw new InvalidDataException();
                            frame.Report.Final = final == 1;
                            ValidateReport(frame.Report);
                        }
                    }
                }
                else if (kind == 2 || kind == 4 || kind == 6)
                {
                    frame.Result = reader.ReadByte(); frame.Index = reader.ReadUInt16();
                    frame.Count = reader.ReadUInt16(); frame.Total = reader.ReadInt32();
                    frame.Hash = reader.ReadBytes(32); int length = reader.ReadUInt16();
                    if (length > StatisticsWire.ChunkBytes) throw new InvalidDataException();
                    frame.Chunk = reader.ReadBytes(length);
                    if (frame.Chunk.Length != length) throw new EndOfStreamException();
                    ValidateReply(frame);
                }
                else throw new InvalidDataException();
                if (stream.Position != stream.Length) throw new InvalidDataException();
                return frame;
            }
        }

        private static void ValidateReport(AccuracyReport report)
        {
            Guid session;
            if (report.Day <= 0 || report.Session == null || !Guid.TryParseExact(report.Session, "N", out session) || session == Guid.Empty)
                throw new InvalidDataException("Ungueltige Treffermeldung.");
            StatisticsWire.ValidateAccuracy(report.Shots, report.Hits);
            if (report.Headshots.HasValue && (report.Headshots.Value < 0 || report.Headshots.Value > report.Hits))
                throw new InvalidDataException("Ungueltige Kopftreffermeldung.");
        }

        private static void ValidateReply(StatisticsMessage frame)
        {
            if (frame.Id <= 0 || frame.Result > 2 || frame.Hash == null || frame.Hash.Length != 32 ||
                frame.Chunk == null || frame.Chunk.Length > StatisticsWire.ChunkBytes)
                throw new InvalidDataException();
            if (frame.Result == 0)
            {
                if (frame.Total <= 0 || frame.Total > StatisticsWire.MaximumBytes ||
                    frame.Count != (frame.Total + StatisticsWire.ChunkBytes - 1) / StatisticsWire.ChunkBytes ||
                    frame.Index < 0 || frame.Index >= frame.Count ||
                    frame.Chunk.Length != Math.Min(StatisticsWire.ChunkBytes,
                        frame.Total - frame.Index * StatisticsWire.ChunkBytes)) throw new InvalidDataException();
            }
            else if (frame.Chunk.Length != 0 || frame.Total != 0 || frame.Index != 0 || frame.Count != 0)
                throw new InvalidDataException();
        }
    }
}
