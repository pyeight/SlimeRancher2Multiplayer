using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Player;

public sealed class PlayerLeavePacket : IPacket
{
    public string PlayerId;

    public PacketType Type { get; init; }
    public PacketReliability Reliability => PacketReliability.ReliableOrdered;

    public void Serialise(PacketWriter writer) => writer.WriteStringWithoutSize(PlayerId);

    public void Deserialise(PacketReader reader) => PlayerId = reader.ReadStringWithSize(15)!;
}