using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Gordo;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.WorldState)]
public sealed class GordoBurstPacket : PacketBase
{
    public string ID { get; set; }

    public override PacketType Type => PacketType.GordoBurst;

    public override void Serialise(PacketWriter writer) => writer.WriteString(ID);

    public override void Deserialise(PacketReader reader) => ID = reader.ReadString();
}