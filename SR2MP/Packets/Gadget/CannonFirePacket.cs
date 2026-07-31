using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Gadget;

internal sealed class CannonFirePacket : IPacket
{
    public long GadgetId;
    public double NextFireTime;

    public PacketType Type => PacketType.CannonFire;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteLong(GadgetId);
        writer.WriteDouble(NextFireTime);
    }

    public void Deserialise(PacketReader reader)
    {
        GadgetId = reader.ReadLong();
        NextFireTime = reader.ReadDouble();
    }
}
