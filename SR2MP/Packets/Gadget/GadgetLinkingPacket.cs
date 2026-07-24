using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Gadget;

internal sealed class GadgetLinkingPacket : IPacket
{
    public long GadgetId;
    public long PartnerId;

    public PacketType Type => PacketType.GadgetLinking;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WritePackedLong(GadgetId);
        writer.WritePackedLong(PartnerId);
    }

    public void Deserialise(PacketReader reader)
    {
        GadgetId = reader.ReadPackedLong();
        PartnerId = reader.ReadPackedLong();
    }
}