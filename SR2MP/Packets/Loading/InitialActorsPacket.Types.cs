using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;
using Unity.Mathematics;

namespace SR2MP.Packets.Loading;

public sealed partial class InitialActorsPacket
{
    public enum ActorType : byte
    {
        Basic = 0,
        
        // Main Actors
        Plort = 1,
        Slime = 2,
        Resource = 3,
        Chicken = 4,
        
        // Gadgets
        Linked = 5,
        LinkedWithAmmo = 6,
        
        // Drones
        RanchDrone = 7,
        ExplorerDrone = 8,
    }
    
    private static Dictionary<ActorType, Type> actorTypes = new()
    {
        { ActorType.Basic, typeof(ActorBase)},
        { ActorType.Slime, typeof(Slime)},
        { ActorType.Plort, typeof(Plort)},
        { ActorType.Resource, typeof(Resource)},
    };
    
    public class ActorBase : INetObject
    {
        public long ActorId { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public int ActorType { get; set; }
        public int Scene { get; set; }

        public virtual void Serialise(PacketWriter writer)
        {
            writer.WriteVector3(Position);
            writer.WriteQuaternion(Rotation);
            writer.WriteLong(ActorId);
            writer.WriteInt(ActorType);
            writer.WriteInt(Scene);
        }

        public virtual void Deserialise(PacketReader reader)
        {
            Position = reader.ReadVector3();
            Rotation = reader.ReadQuaternion();
            ActorId = reader.ReadLong();
            ActorType = reader.ReadInt();
            Scene = reader.ReadInt();
        }
    }

    public sealed class Slime : ActorBase
    {
        public float4 Emotions { get; set; }

        public override void Serialise(PacketWriter writer)
        {
            base.Serialise(writer);
            writer.WriteFloat4(Emotions);
        }
        
        public override void Deserialise(PacketReader reader)
        {
            base.Deserialise(reader);
            Emotions = reader.ReadFloat4();
        }
    }
    public sealed class Plort : ActorBase
    {
        public bool Invulnerable { get; set; }
        public float InvulnerablePeriod { get; set; }
        
        public double DestroyTime { get; set; }

        public override void Serialise(PacketWriter writer)
        {
            base.Serialise(writer);
            writer.WriteBool(Invulnerable);
            writer.WriteFloat(InvulnerablePeriod);
            writer.WriteDouble(DestroyTime);
        }
        
        public override void Deserialise(PacketReader reader)
        {
            base.Deserialise(reader);
            Invulnerable = reader.ReadBool();
            InvulnerablePeriod = reader.ReadFloat();
            DestroyTime = reader.ReadDouble();
        }
    }
    public sealed class Resource : ActorBase
    {
        public double DestroyTime { get; set; }

        public override void Serialise(PacketWriter writer)
        {
            base.Serialise(writer);
            writer.WriteDouble(DestroyTime);
        }
        
        public override void Deserialise(PacketReader reader)
        {
            base.Deserialise(reader);
            DestroyTime = reader.ReadDouble();
        }
    }
}