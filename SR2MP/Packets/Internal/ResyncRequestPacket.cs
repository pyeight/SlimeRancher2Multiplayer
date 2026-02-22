using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Internal;

public readonly struct ResyncRequestPacket : IPacket
{
    public PacketType Type => PacketType.ResyncRequest;
    public PacketReliability Reliability => PacketReliability.ReliableOrdered;

    public void Serialise(PacketWriter writer) { }

    public void Deserialise(PacketReader reader) { }
}
