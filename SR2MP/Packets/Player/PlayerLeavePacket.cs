using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Player;

public sealed class PlayerLeavePacket : IPacket
{
    public string PlayerId { get; set; }
    public PacketType Type { get; set; }
    public PacketReliability Reliability => PacketReliability.ReliableOrdered;

    public void Serialise(PacketWriter writer) => writer.WriteString(PlayerId);

    public void Deserialise(PacketReader reader) => PlayerId = reader.ReadString();
}