using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Actor;

public sealed class ResourceAttachPacket : IPacket
{
    public ActorId ActorId;
    public string PlotID;
    public int Joint;
    public Vector3 SpawnerID;

    public SpawnResourceModel Model;

    public PacketType Type => PacketType.ResourceAttach;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteLong(ActorId.Value);
        writer.WriteString(PlotID);
        writer.WriteInt(Joint);
        writer.WriteVector3(SpawnerID);

        writer.WriteBool(Model.nextSpawnRipens);
        writer.WriteDouble(Model.nextSpawnTime);
        writer.WriteFloat(Model.storedWater);
        writer.WriteBool(Model.wasPreviouslyPlanted);
    }

    public void Deserialise(PacketReader reader)
    {
        ActorId = new ActorId(reader.ReadLong());
        PlotID = reader.ReadString();
        Joint = reader.ReadInt();
        SpawnerID = reader.ReadVector3();

        Model = new SpawnResourceModel
        {
            nextSpawnRipens = reader.ReadBool(),
            nextSpawnTime = reader.ReadDouble(),
            storedWater = reader.ReadFloat(),
            wasPreviouslyPlanted = reader.ReadBool(),
        };
    }
}