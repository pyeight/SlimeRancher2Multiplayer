using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.WorldState)]
public sealed class InitialMapPacket : PacketBase
{
    public override PacketType Type => PacketType.InitialMap;

    public List<string> UnlockedNodes { get; set; }

    // todo: Add navigation marker data later.

    public override void Serialise(PacketWriter writer)
    {
        writer.WriteList(UnlockedNodes, PacketWriterDels.String);
    }

    public override void Deserialise(PacketReader reader)
    {
        UnlockedNodes = reader.ReadList(PacketReaderDels.String);
    }
}