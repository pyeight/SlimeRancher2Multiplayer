using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class UpgradesPacket : IPacket
{
    public Dictionary<byte, sbyte> Upgrades { get; set; }

    public PacketType Type => PacketType.InitialPlayerUpgrades;

    public void Serialise(PacketWriter writer) => writer.WriteDictionary(Upgrades, PacketWriterDels.Byte, PacketWriterDels.SByte);

    public void Deserialise(PacketReader reader) => Upgrades = reader.ReadDictionary(PacketReaderDels.Byte, PacketReaderDels.SByte);
}