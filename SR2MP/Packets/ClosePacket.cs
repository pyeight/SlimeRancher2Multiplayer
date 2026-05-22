using SR2MP.Packets.Utils;

namespace SR2MP.Packets;

internal readonly struct ClosePacket : IPacket
{
    public readonly PacketType Type => PacketType.Close;
    public readonly PacketReliability Reliability => PacketReliability.Unreliable;
    public readonly NetworkChannel Channel => NetworkChannel.Important;

    public void Serialise(PacketWriter writer) { }

    public void Deserialise(PacketReader reader) { }
}