using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class UpgradesPacket : IPacket
{
    public byte Type { get; set; }

    public Dictionary<byte, sbyte> Upgrades { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteDictionary(Upgrades, PacketWriterDels.Byte, PacketWriterDels.SByte);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        Upgrades = reader.ReadDictionary(PacketReaderDels.Byte, PacketReaderDels.SByte);
    }
}