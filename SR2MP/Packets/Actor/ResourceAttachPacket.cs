using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Actor;

internal sealed class ResourceAttachPacket : IPacket
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
        writer.WritePackedLong(ActorId.Value);
        writer.WriteString(PlotID);
        writer.WriteInt(Joint);
        writer.WriteVector3(SpawnerID);

        writer.WriteDouble(Model.nextSpawnTime);
        writer.WriteFloat(Model.storedWater);
        writer.WritePackedBool(Model.nextSpawnRipens);
        writer.WritePackedBool(Model.wasPreviouslyPlanted);
    }

    public void Deserialise(PacketReader reader)
    {
        ActorId = new ActorId(reader.ReadPackedLong());
        PlotID = reader.ReadPooledString()!;
        Joint = reader.ReadInt();
        SpawnerID = reader.ReadVector3();

        Model = new SpawnResourceModel
        {
            nextSpawnTime = reader.ReadDouble(),
            storedWater = reader.ReadFloat(),
            nextSpawnRipens = reader.ReadPackedBool(),
            wasPreviouslyPlanted = reader.ReadPackedBool()
        };
    }
}