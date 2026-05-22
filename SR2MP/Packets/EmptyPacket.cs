using SR2MP.Packets.Utils;

namespace SR2MP.Packets;

internal readonly struct EmptyPacket : IPacket
{
    public readonly PacketType Type { get; init; }
    public readonly PacketReliability Reliability { get; init; }
    public readonly NetworkChannel Channel { get; init; }

    public void Serialise(PacketWriter writer) { }

    public void Deserialise(PacketReader reader) { }
}