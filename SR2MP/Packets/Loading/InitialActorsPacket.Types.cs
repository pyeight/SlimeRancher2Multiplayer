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
        ExplorerDrone = 9
    }

    private static Dictionary<ActorType, Type> actorTypes = new(ActorTypeComparer.Instance)
    {
        { ActorType.Basic, typeof(ActorBase) },
        { ActorType.Slime, typeof(Slime) },
        { ActorType.Plort, typeof(Plort) },
        { ActorType.Resource, typeof(Resource) },
        { ActorType.Gadget, typeof(ActorBase) },
    };

    public class ActorBase : INetObject
    {
        public long ActorId;
        public Vector3 Position;
        public Quaternion Rotation;
        public int ActorTypeId;
        public int Scene;

        protected virtual ActorType Type => ActorType.Basic;

        public virtual void Serialise(PacketWriter writer)
        {
            writer.WriteEnum(Type);
            writer.WriteVector3(Position);
            writer.WriteQuaternion(Rotation);
            writer.WritePackedLong(ActorId);
            writer.WritePackedInt(ActorTypeId);
            writer.WritePackedInt(Scene);
        }

        public virtual void Deserialise(PacketReader reader)
        {
            // Type is already deserialised here
            Position = reader.ReadVector3();
            Rotation = reader.ReadQuaternion();
            ActorId = reader.ReadPackedLong();
            ActorTypeId = reader.ReadPackedInt();
            Scene = reader.ReadPackedInt();
        }
    }

    public sealed class Slime : ActorBase
    {
        public float4 Emotions;

        protected override ActorType Type => ActorType.Slime;

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

    public abstract class Destroyable : ActorBase
    {
        public double DestroyTime;

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

    public sealed class Resource : Destroyable
    {
        public double ProgressTime;
        public ResourceCycle.State ResourceState;

        protected override ActorType Type => ActorType.Resource;

        public override void Serialise(PacketWriter writer)
        {
            base.Serialise(writer);
            writer.WriteDouble(ProgressTime);
            writer.WritePackedEnum(ResourceState);
        }

        public override void Deserialise(PacketReader reader)
        {
            base.Deserialise(reader);
            ProgressTime = reader.ReadDouble();
            ResourceState = reader.ReadPackedEnum<ResourceCycle.State>();
        }
    }

    public sealed class Plort : Destroyable
    {
        public bool Invulnerable;
        public float InvulnerablePeriod;

        protected override ActorType Type => ActorType.Plort;

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

    private sealed class ActorTypeComparer : IEqualityComparer<ActorType>
    {
        public static readonly ActorTypeComparer Instance = new();

        public bool Equals(ActorType x, ActorType y) => x == y;

        public int GetHashCode(ActorType obj) => (int)obj;
    }
}