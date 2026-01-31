using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Player;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.Control)]
public sealed class PlayerJoinPacket : PacketBase
{
    public string PlayerId { get; set; }
    public string? PlayerName { get; set; }
    public PacketType Kind { get; set; } = PacketType.PlayerJoin;
    public override PacketType Type => Kind;

    public override void Serialise(PacketWriter writer)
    {
        writer.WriteString(PlayerId);
        writer.WriteString(PlayerName ?? "No Name Set");
    }

    public override void Deserialise(PacketReader reader)
    {
        PlayerId = reader.ReadString();
        PlayerName = reader.ReadString();
    }
}
