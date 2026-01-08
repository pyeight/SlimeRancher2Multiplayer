using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class SPediasPacket : IPacket
{
    public byte Type { get; set; }

    public List<string> Entries { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteList(Entries, PacketWriterDels.String);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        Entries = reader.ReadList(PacketReaderDels.String);
    }
}