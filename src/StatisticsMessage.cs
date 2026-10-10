using System;
using System.IO;

namespace ModStatistik
{
    internal sealed class StatisticsMessage
    {
        internal const string Prefix = "[ModStatistik:1]";
        internal const int MaximumTextLength = 12000;
        internal bool IsRequest;
        internal int Id, Index, Count, Total;
        internal byte Result;
        internal byte[] Hash, Chunk;

        internal static bool IsReserved(string message)
        { return message != null && message.StartsWith(Prefix, StringComparison.Ordinal); }

        internal static string Request(int id, byte[] hash)
        {
            if (id <= 0 || (hash != null && hash.Length != 32)) throw new InvalidDataException();
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((byte)1); writer.Write(id); writer.Write(hash ?? new byte[32]);
                return Prefix + Convert.ToBase64String(stream.ToArray());
            }
        }

        internal static string Reply(int id, byte result, int index, int count, int total,
            byte[] hash, byte[] chunk)
        {
            var frame = new StatisticsMessage { Id = id, Result = result, Index = index,
                Count = count, Total = total, Hash = hash ?? new byte[32], Chunk = chunk };
            ValidateReply(frame);
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((byte)2); writer.Write(id); writer.Write(result);
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
                if (kind == 1)
                {
                    frame.IsRequest = true; frame.Hash = reader.ReadBytes(32);
                    if (frame.Hash.Length != 32) throw new InvalidDataException();
                }
                else if (kind == 2)
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
