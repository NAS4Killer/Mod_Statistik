using System;
using System.IO;
using ModStatistik;

public sealed class NetPackageStatisticsRequest : NetPackage
{
    private int request;
    private byte[] hash;
    public override NetPackageDirection PackageDirection { get { return NetPackageDirection.ToServer; } }
    public override bool ReliableDelivery { get { return true; } }
    public NetPackageStatisticsRequest Setup(int id, byte[] knownHash)
    {
        request = id; hash = knownHash == null ? new byte[32] : (byte[])knownHash.Clone(); return this;
    }
    public override void write(PooledBinaryWriter writer)
    {
        base.write(writer); writer.Write((byte)1); writer.Write(request); writer.Write(hash);
    }
    public override void read(PooledBinaryReader reader)
    {
        if (reader.ReadByte() != 1) throw new InvalidDataException("Unbekanntes Statistikprotokoll.");
        request = reader.ReadInt32(); hash = reader.ReadBytes(32);
        if (request <= 0 || hash.Length != 32) throw new InvalidDataException("Ungueltige Statistikanfrage.");
    }
    public override void ProcessPackage(World world, GameManager callbacks) { StatisticsSync.HandleRequest(Sender, request, hash); }
}

public sealed class NetPackageStatisticsReply : NetPackage
{
    private int request, index, count, total;
    private byte result;
    private byte[] hash, chunk;
    public override NetPackageDirection PackageDirection { get { return NetPackageDirection.ToClient; } }
    public override bool ReliableDelivery { get { return true; } }
    public NetPackageStatisticsReply Setup(int id, byte kind, int part, int parts, int bytes, byte[] digest, byte[] data)
    {
        request = id; result = kind; index = part; count = parts; total = bytes;
        hash = digest == null ? new byte[32] : (byte[])digest.Clone(); chunk = (byte[])data.Clone(); return this;
    }
    public override void write(PooledBinaryWriter writer)
    {
        base.write(writer); writer.Write((byte)1); writer.Write(request); writer.Write(result);
        writer.Write((ushort)index); writer.Write((ushort)count); writer.Write(total);
        writer.Write(hash); writer.Write((ushort)chunk.Length); writer.Write(chunk);
    }
    public override void read(PooledBinaryReader reader)
    {
        if (reader.ReadByte() != 1) throw new InvalidDataException("Unbekanntes Statistikprotokoll.");
        request = reader.ReadInt32(); result = reader.ReadByte(); index = reader.ReadUInt16(); count = reader.ReadUInt16();
        total = reader.ReadInt32(); hash = reader.ReadBytes(32); int length = reader.ReadUInt16();
        if (request <= 0 || result > 2 || hash.Length != 32 || length > StatisticsWire.ChunkBytes)
            throw new InvalidDataException("Ungueltiges Statistikpaket.");
        if (result == 0 && (total <= 0 || total > StatisticsWire.MaximumBytes || count != (total + StatisticsWire.ChunkBytes - 1) / StatisticsWire.ChunkBytes ||
            index >= count || length != Math.Min(StatisticsWire.ChunkBytes, total - index * StatisticsWire.ChunkBytes)))
            throw new InvalidDataException("Ungueltige Statistikteilgroesse.");
        if (result != 0 && (length != 0 || total != 0 || index != 0 || count != 0)) throw new InvalidDataException("Ungueltige Statistikantwort.");
        chunk = reader.ReadBytes(length);
        if (chunk.Length != length) throw new EndOfStreamException();
    }
    public override void ProcessPackage(World world, GameManager callbacks)
    {
        StatisticsSync.HandleReply(request, result, index, count, total, hash, chunk);
    }
}
