using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Gordo;

public struct GordoBurstPacket : IPacket
{
    public byte Type { get; set; }

    public string ID { get; set; }

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteString(ID);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        ID = reader.ReadString();
    }
}