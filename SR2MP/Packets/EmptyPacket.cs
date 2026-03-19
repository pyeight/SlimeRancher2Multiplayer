using SR2MP.Packets.Utils;

namespace SR2MP.Packets;

public readonly struct EmptyPacket : IPacket
{
    public PacketType Type { get; init; }
    public PacketReliability Reliability { get; init; }

    public void Serialise(PacketWriter writer) { }

    public void Deserialise(PacketReader reader) { }
}