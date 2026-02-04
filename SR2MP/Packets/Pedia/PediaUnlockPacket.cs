using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Pedia;

public sealed class PediaUnlockPacket : IPacket
{
    public string ID { get; set; }
    public bool Popup { get; set; }

    public PacketType Type => PacketType.PediaUnlock;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(ID);
        writer.WriteBool(Popup);
    }

    public void Deserialise(PacketReader reader)
    {
        ID = reader.ReadString();
        Popup = reader.ReadBool();
    }
}