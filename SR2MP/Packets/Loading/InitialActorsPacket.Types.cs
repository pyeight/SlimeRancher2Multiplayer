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
        Gadget = 5,
        LinkedGadget = 6,
        LinkedGadgetWithAmmo = 7,

        // Drones
        RanchDrone = 8,
        ExplorerDrone = 9,
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

        protected virtual ActorType Type => InitialActorsPacket.ActorType.Basic;

        public virtual void Serialise(PacketWriter writer)
        {
            writer.WriteEnum(Type);
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

        protected override ActorType Type => InitialActorsPacket.ActorType.Slime;

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

    public class Resource : ActorBase
    {
        public double DestroyTime { get; set; }
        public double ProgressTime { get; set; }

        public ResourceCycle.State ResourceState { get; set; }
        
        protected override ActorType Type => InitialActorsPacket.ActorType.Resource;

        public override void Serialise(PacketWriter writer)
        {
            base.Serialise(writer);
            writer.WriteDouble(DestroyTime);
            writer.WriteDouble(ProgressTime);
            writer.WriteEnum(ResourceState);
        }

        public override void Deserialise(PacketReader reader)
        {
            base.Deserialise(reader);
            DestroyTime = reader.ReadDouble();
            ProgressTime = reader.ReadDouble();
            ResourceState = reader.ReadEnum<ResourceCycle.State>();
        }
    }

    public sealed class Plort  : ActorBase
    {
        public double DestroyTime { get; set; }
        public bool Invulnerable { get; set; }
        public float InvulnerablePeriod { get; set; }

        protected override ActorType Type => InitialActorsPacket.ActorType.Plort;

        public override void Serialise(PacketWriter writer)
        {
            base.Serialise(writer);
            writer.WriteBool(Invulnerable);
            writer.WriteFloat(InvulnerablePeriod);
        }

        public override void Deserialise(PacketReader reader)
        {
            base.Deserialise(reader);
            Invulnerable = reader.ReadBool();
            InvulnerablePeriod = reader.ReadFloat();
        }
    }
}