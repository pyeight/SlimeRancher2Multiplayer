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
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(ID);
        writer.WritePackedInt(NewFoodCount);
        writer.WritePackedInt(RequiredFoodCount);
        writer.WritePackedInt(GordoType);
    }

    public void Deserialise(PacketReader reader)
    {
        ID = reader.ReadString();
        NewFoodCount = reader.ReadPackedInt();
        RequiredFoodCount = reader.ReadPackedInt();
        GordoType = reader.ReadPackedInt();
    }
}