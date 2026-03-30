using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

internal sealed class ConnectionDenyPacket : IPacket
{
    public string Reason;

    public PacketType Type => PacketType.ConnectionDeny;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer) => writer.WriteString(Reason);

    public void Deserialise(PacketReader reader) => Reason = reader.ReadString()!;
}