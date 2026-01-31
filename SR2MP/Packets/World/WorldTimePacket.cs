using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.Sequenced, channel: SR2MP.Networking.NetChannels.WorldState)]
public sealed class WorldTimePacket : PacketBase
{
    public double Time { get; set; }
    public override PacketType Type => PacketType.WorldTime;

    public override void Serialise(PacketWriter writer) => writer.WriteDouble(Time);

    public override void Deserialise(PacketReader reader) => Time = reader.ReadDouble();
}
