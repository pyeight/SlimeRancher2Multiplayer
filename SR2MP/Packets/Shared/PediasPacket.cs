using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared;

public sealed class PediasPacket : IPacket
{
    public byte Type { get; set; }

    public List<string> Entries { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteList(Entries, (writer2, value) => writer2.WriteString(value));
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        Entries = reader.ReadList(reader2 => reader2.ReadString());
    }
}