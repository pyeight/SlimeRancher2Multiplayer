using SR2MP.Packets.Utils;

namespace SR2MP.Packets.LandPlots;

public sealed class GardenUpdatePacket : IPacket
{
    public string GardenID;
    public double NextSpawnTime;
    public float StoredWater;
    public bool NextSpawnRipens;

    public PacketType Type => PacketType.GardenUpdate;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(GardenID);
        writer.WriteDouble(NextSpawnTime);
        writer.WriteFloat(StoredWater);
        writer.WriteBool(NextSpawnRipens);
    }

    public void Deserialise(PacketReader reader)
    {
        GardenID = reader.ReadString();
        NextSpawnTime = reader.ReadDouble();
        StoredWater = reader.ReadFloat();
        NextSpawnRipens = reader.ReadBool();
    }
}