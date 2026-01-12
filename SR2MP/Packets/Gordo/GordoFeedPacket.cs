using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Gordo;

public sealed class GordoFeedPacket : IPacket
{
    public string ID { get; set; }
    public int NewFoodCount { get; set; }

    // Needed for unregistered gordos.
    public int RequiredFoodCount { get; set; }
    public int GordoType { get; set; }

    public PacketType Type => PacketType.GordoFeed;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(ID);
        writer.WriteInt(NewFoodCount);
        writer.WriteInt(RequiredFoodCount);
        writer.WriteInt(GordoType);
    }

    public void Deserialise(PacketReader reader)
    {
        ID = reader.ReadString();
        NewFoodCount = reader.ReadInt();
        RequiredFoodCount = reader.ReadInt();
        GordoType = reader.ReadInt();
    }
}