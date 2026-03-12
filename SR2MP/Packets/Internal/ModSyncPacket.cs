using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Internal;

public sealed class ModSyncPacket : IPacket
{
    public Dictionary<ushort, string> Mods;

    public PacketType Type => PacketType.ModSyncAck;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteDictionary(Mods, PacketWriterDels.UShort, PacketWriterDels.String);
    }

    public void Deserialise(PacketReader reader)
    {
        Mods = reader.ReadDictionary(PacketReaderDels.UShort, PacketReaderDels.String);
    }
}