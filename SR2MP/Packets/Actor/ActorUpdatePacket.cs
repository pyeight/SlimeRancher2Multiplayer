using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;
using Unity.Mathematics;

namespace SR2MP.Packets.Actor;

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
        writer.WriteLong(ActorId.Value);
        writer.WriteByte((byte)UpdateType);
        
        if (UpdateType == ActorUpdateType.Slime)
        {
            writer.WriteVector3(Position);
            writer.WriteQuaternion(Rotation);
            writer.WriteVector3(Velocity);
            writer.WriteFloat4(Emotions);
        }
        else if (UpdateType == ActorUpdateType.Resource)
        {
            writer.WriteVector3(Position);
            writer.WriteQuaternion(Rotation);
            writer.WriteVector3(Velocity);
            writer.WriteDouble(ResourceProgress);
            writer.WriteEnum(ResourceState);
        }
        else if (UpdateType == ActorUpdateType.Plort)
        {
            writer.WriteVector3(Position);
            writer.WriteQuaternion(Rotation);
            writer.WriteVector3(Velocity);
            writer.WriteBool(Invulnerable);
            writer.WriteFloat(InvulnerablePeriod);
        }
        else
        {
            writer.WriteVector3(Position);
            writer.WriteQuaternion(Rotation);
            writer.WriteVector3(Velocity);
        }
     }
     
    public void Deserialise(PacketReader reader)
    {
        ActorId = new ActorId(reader.ReadLong());
        UpdateType = (ActorUpdateType)reader.ReadByte();

        if (UpdateType == ActorUpdateType.Slime)
        {
            Position = reader.ReadVector3();
            Rotation = reader.ReadQuaternion();
            Velocity = reader.ReadVector3();
            Emotions = reader.ReadFloat4();
        }
        else if (UpdateType == ActorUpdateType.Resource)
        { 
            Position = reader.ReadVector3();
            Rotation = reader.ReadQuaternion();
            Velocity = reader.ReadVector3();
            ResourceProgress = reader.ReadDouble();
            ResourceState = reader.ReadEnum<ResourceCycle.State>();
        }
        else if (UpdateType == ActorUpdateType.Plort)
        {
            Position = reader.ReadVector3();
            Rotation = reader.ReadQuaternion();
            Velocity = reader.ReadVector3();
            Invulnerable = reader.ReadBool();
            InvulnerablePeriod = reader.ReadFloat();
        }
        else
        {
            Position = reader.ReadVector3();
            Rotation = reader.ReadQuaternion();
            Velocity = reader.ReadVector3();
        }
    }
}