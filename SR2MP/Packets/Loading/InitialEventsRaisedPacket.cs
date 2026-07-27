using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

internal sealed class InitialEventsRaisedPacket : IPacket
{
    internal sealed class Entry : INetObject
    {
        public string EventKey;
        public string DataKey;

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

    public List<Entry> Entries;

    public PacketType Type => PacketType.InitialEventsRaised;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer) => writer.WriteList(Entries, PacketWriterDels.NetObject<Entry>.Writer);

    public void Deserialise(PacketReader reader) => Entries = reader.ReadList(PacketReaderDels.NetObject<Entry>.Reader)!;
}
