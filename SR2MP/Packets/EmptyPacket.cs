using SR2MP.Packets.Utils;

namespace SR2MP.Packets;

public struct EmptyPacket : IPacket
{
    public PacketType Type { get; init; }
    public readonly PacketReliability Reliability => PacketReliability.Reliable;

    public readonly void Serialise(PacketWriter writer) { }

    public readonly void Deserialise(PacketReader reader) { }
}