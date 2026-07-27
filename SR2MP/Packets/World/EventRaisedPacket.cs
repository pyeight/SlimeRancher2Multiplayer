using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

internal sealed class EventRaisedPacket : IPacket
{
    public string EventKey;
    public string DataKey;

    public PacketType Type => PacketType.EventRaised;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(EventKey);
        writer.WriteString(DataKey);
    }

    public void Deserialise(PacketReader reader)
    {
        EventKey = reader.ReadPooledString()!;
        DataKey = reader.ReadPooledString()!;
    }
}
