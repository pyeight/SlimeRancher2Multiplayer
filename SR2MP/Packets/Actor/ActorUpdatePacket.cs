using System.Runtime.InteropServices;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;
using Unity.Mathematics;

namespace SR2MP.Packets.Actor;

[StructLayout(LayoutKind.Auto)]
public struct ActorUpdatePacket : IPacket
{
    public ActorId ActorId;

    public Quaternion Rotation;
    public Vector3 Position;
    public Vector3 Velocity;

    public float4 Emotions;

    public double ResourceProgress;
    public ResourceCycle.State ResourceState;

    public bool Invulnerable;
    public float InvulnerablePeriod;

    public ActorUpdateType UpdateType;

    public readonly PacketType Type => PacketType.ActorUpdate;
    public readonly PacketReliability Reliability => PacketReliability.Unreliable;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WritePackedLong(ActorId.Value);
        writer.WriteEnum(UpdateType);
        writer.WriteVector3(Position);
        writer.WriteQuaternion(Rotation);
        writer.WriteVector3(Velocity);

        if (UpdateType == ActorUpdateType.Slime)
        {
            writer.WriteFloat4(Emotions);
        }
        else if (UpdateType == ActorUpdateType.Resource)
        {
            writer.WriteDouble(ResourceProgress);
            writer.WritePackedEnum(ResourceState);
        }
        else if (UpdateType == ActorUpdateType.Plort)
        {
            writer.WriteBool(Invulnerable);
            writer.WriteFloat(InvulnerablePeriod);
        }
    }

    public void Deserialise(PacketReader reader)
    {
        ActorId = new ActorId(reader.ReadPackedLong());
        UpdateType = reader.ReadEnum<ActorUpdateType>();
        Position = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
        Velocity = reader.ReadVector3();

        if (UpdateType == ActorUpdateType.Slime)
        {
            Emotions = reader.ReadFloat4();
        }
        else if (UpdateType == ActorUpdateType.Resource)
        {
            ResourceProgress = reader.ReadDouble();
            ResourceState = reader.ReadPackedEnum<ResourceCycle.State>();
        }
        else if (UpdateType == ActorUpdateType.Plort)
        {
            Invulnerable = reader.ReadBool();
            InvulnerablePeriod = reader.ReadFloat();
        }
    }
}