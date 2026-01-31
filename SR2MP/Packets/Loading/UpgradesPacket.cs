using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.WorldState)]
public sealed class UpgradesPacket : PacketBase
{
    public Dictionary<byte, sbyte> Upgrades { get; set; }

    public override PacketType Type => PacketType.InitialPlayerUpgrades;

    public override void Serialise(PacketWriter writer) => writer.WriteDictionary(Upgrades, PacketWriterDels.Byte, PacketWriterDels.SByte);

    public override void Deserialise(PacketReader reader) => Upgrades = reader.ReadDictionary(PacketReaderDels.Byte, PacketReaderDels.SByte);
}