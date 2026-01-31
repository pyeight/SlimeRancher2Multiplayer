using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.Control)]
public sealed class ClosePacket : PacketBase
{
    public override PacketType Type => PacketType.Close;

    public override void Serialise(PacketWriter writer) { }

    public override void Deserialise(PacketReader reader) { }
}
