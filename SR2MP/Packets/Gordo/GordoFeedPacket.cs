using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Gordo;

public struct GordoFeedPacket : IPacket
{
    public byte Type { get; set; }

    public string ID { get; set; }
    public int NewFoodCount { get; set; }

    // Needed for unregistered gordos.
    public int RequiredFoodCount { get; set; }
    public int GordoType { get; set; }

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteString(ID);
        writer.WriteInt(NewFoodCount);
        writer.WriteInt(RequiredFoodCount);
        writer.WriteInt(GordoType);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        ID = reader.ReadString();
        NewFoodCount = reader.ReadInt();
        RequiredFoodCount = reader.ReadInt();
        GordoType = reader.ReadInt();
    }
}