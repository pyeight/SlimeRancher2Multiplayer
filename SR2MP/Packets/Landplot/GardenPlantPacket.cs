using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Landplot;

public sealed class GardenPlantPacket : IPacket
{
    public string ID { get; set; }
    public int ActorType { get; set; }

    public PacketType Type => PacketType.GardenPlant;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(ID);
        writer.WritePackedInt(ActorType);
    }

    public void Deserialise(PacketReader reader)
    {
        ID = reader.ReadString();
        ActorType = reader.ReadPackedInt();
    }
}