using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class PediasPacket : IPacket
{
    public List<string> Entries { get; set; }

    public PacketType Type => PacketType.InitialPediaEntries;

    public void Serialise(PacketWriter writer) => writer.WriteList(Entries, PacketWriterDels.String);

    public void Deserialise(PacketReader reader) => Entries = reader.ReadList(PacketReaderDels.String);
}