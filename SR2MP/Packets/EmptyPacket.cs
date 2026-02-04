using SR2MP.Packets.Utils;

namespace SR2MP.Packets;

public struct EmptyPacket : IPacket
{
    public PacketType Type { get; set; }
    public PacketReliability Reliability { get; set; }

    public readonly void Serialise(PacketWriter writer) { }

    public readonly void Deserialise(PacketReader reader) { }
}