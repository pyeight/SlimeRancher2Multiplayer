using SR2MP.Packets.Utils;

namespace SR2MP.Packets;

public readonly struct ClosePacket : IPacket
{
    public PacketType Type => PacketType.Close;
    public PacketReliability Reliability => PacketReliability.Unreliable;

    public void Serialise(PacketWriter writer) { }

    public void Deserialise(PacketReader reader) { }
}