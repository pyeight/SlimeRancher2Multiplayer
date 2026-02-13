using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;
using Unity.Mathematics;

namespace SR2MP.Packets.Actor;

public struct ActorUpdatePacket : IPacket
{
    public ActorId ActorId { get; set; }
    public double ResourceProgress { get; set; }

    public Quaternion Rotation { get; set; }
    public float4 Emotions { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }

    public ResourceCycle.State ResourceState { get; set; }

    public readonly PacketType Type => PacketType.ActorUpdate;
    public readonly PacketReliability Reliability => PacketReliability.Unreliable;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteLong(ActorId.Value);
        writer.WriteVector3(Position);
        writer.WriteQuaternion(Rotation);
        writer.WriteVector3(Velocity);
        writer.WriteFloat4(Emotions);
        writer.WriteDouble(ResourceProgress);
        writer.WriteEnum(ResourceState);
    }

    public void Deserialise(PacketReader reader)
    {
        ActorId = new ActorId(reader.ReadLong());
        Position = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
        Velocity = reader.ReadVector3();
        Emotions = reader.ReadFloat4();
        ResourceProgress = reader.ReadDouble();
        ResourceState = reader.ReadEnum<ResourceCycle.State>();
    }
}