using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared;

public sealed class PediaUnlockPacket : IPacket
{
    public byte Type { get; set; }

    public string ID { get; set; }
    public bool Popup { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);

        writer.WriteString(ID);
        writer.WriteBool(Popup);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();

        ID = reader.ReadString();
        Popup = reader.ReadBool();
    }
}