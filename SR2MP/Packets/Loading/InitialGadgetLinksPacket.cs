using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

internal sealed class InitialGadgetLinksPacket : IPacket
{
    internal sealed class Link : INetObject
    {
        public long GadgetId;
        public long PartnerId;

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

    public List<Link> Links;

    public PacketType Type => PacketType.InitialGadgetLinks;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer) => writer.WriteList(Links, PacketWriterDels.NetObject<Link>.Writer);

    public void Deserialise(PacketReader reader) => Links = reader.ReadList(PacketReaderDels.NetObject<Link>.Reader)!;
}