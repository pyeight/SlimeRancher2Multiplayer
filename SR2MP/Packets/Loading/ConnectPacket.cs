using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.Control)]
public sealed class ConnectPacket : PacketBase
{
    public string PlayerId { get; set; }
    public string Username { get; set; }

    public override PacketType Type => PacketType.Connect;

    public override void Serialise(PacketWriter writer)
    {
        writer.WriteString(PlayerId);
        writer.WriteString(Username);
    }

    public override void Deserialise(PacketReader reader)
    {
        PlayerId = reader.ReadString();
        Username = reader.ReadString();
    }
}