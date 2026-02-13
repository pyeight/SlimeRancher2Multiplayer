using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class InitialPediaPacket : IPacket
{
    public List<string> Entries;

    public PacketType Type => PacketType.InitialPediaEntries;
    public PacketReliability Reliability => PacketReliability.ReliableOrdered;

    public void Serialise(PacketWriter writer) => writer.WriteList(Entries, PacketWriterDels.String);

    public void Deserialise(PacketReader reader) => Entries = reader.ReadList(PacketReaderDels.String);
}