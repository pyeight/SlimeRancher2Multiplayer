using SR2MP.Packets.Utils;

namespace SR2MP.Packets;

public struct ClosePacket : IPacket
{
    public readonly PacketType Type => PacketType.Close;

    public readonly void Serialise(PacketWriter writer) { }

    public readonly void Deserialise(PacketReader reader) { }
}