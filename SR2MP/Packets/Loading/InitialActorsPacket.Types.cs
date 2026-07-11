using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Drone;
using SR2MP.Packets.Ammo;
using SR2MP.Packets.Utils;
using Unity.Mathematics;

namespace SR2MP.Packets.Loading;

internal partial class InitialActorsPacket
{
    internal enum ActorType : byte
    {
        Basic = 0,
        
        // Main Actors
        Plort = 1,
        Slime = 2,
        Resource = 3,
        Chicken = 4,
        Sprinkle = 5,
        
        // Gadgets
        Gadget = 6,
        LinkedGadget = 7,
        LinkedGadgetWithAmmo = 8,
        
        // Drones
        DroneStation = 9,
    } 
    
    private static Dictionary<ActorType, Type> actorTypes = new(ActorTypeComparer.Instance)
    {
        { ActorType.Basic,                typeof(ActorBase) }, 
        
        { ActorType.Slime,                typeof(Slime) },
        { ActorType.Plort,                typeof(Plort) },
        { ActorType.Resource,             typeof(Resource) },
        { ActorType.Sprinkle,             typeof(Sprinkle) },
        
        { ActorType.Gadget,               typeof(Gadget) },
        { ActorType.LinkedGadget,         typeof(LinkedGadget) },
        { ActorType.LinkedGadgetWithAmmo, typeof(LinkedAmmoGadget) },
        
        { ActorType.DroneStation,         typeof(DroneStation) },
    };

    internal class ActorBase : INetObject
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
            Position = reader.ReadVector3();
            Rotation = reader.ReadQuaternion();
            ActorId = reader.ReadPackedLong();
            ActorTypeId = reader.ReadPackedInt();
            Scene = reader.ReadPackedInt();
        }
    }
    
    internal abstract class Destroyable : ActorBase
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
    
    internal sealed class Slime : ActorBase
    {
        public float4 Emotions;
        public bool Sleeping;
        public int Radiancy;
        public SlimeAppearance.AppearanceSaveSet FirstAppearance;
        public SlimeAppearance.AppearanceSaveSet SecondAppearance;

        protected override ActorType Type => ActorType.Slime;

        public override void Serialise(PacketWriter writer)
        {
            base.Serialise(writer);
            writer.WriteFloat4(Emotions);
            writer.WriteBool(Sleeping);
            writer.WritePackedInt(Radiancy);
            writer.WritePackedEnum(FirstAppearance);
            writer.WritePackedEnum(SecondAppearance);
        }

        public override void Deserialise(PacketReader reader)
        {
            base.Deserialise(reader);
            Emotions = reader.ReadFloat4();
            Sleeping = reader.ReadBool();
            Radiancy = reader.ReadPackedInt();
            FirstAppearance = reader.ReadPackedEnum<SlimeAppearance.AppearanceSaveSet>();
            SecondAppearance = reader.ReadPackedEnum<SlimeAppearance.AppearanceSaveSet>();
        }
    }
    
    internal sealed class Plort : Destroyable
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
    
    internal sealed class Resource : Destroyable
    {
        public double ProgressTime;
        public ResourceCycle.State ResourceState;
        
        public int JointIndex = -1;
        public string PlotID = string.Empty;
        public Vector3 SpawnerPosition;
        public float Scale = 1f;
        
        protected override ActorType Type => ActorType.Resource;
        
        public override void Serialise(PacketWriter writer)
        {
            base.Serialise(writer);
            writer.WriteDouble(ProgressTime);
            writer.WritePackedEnum(ResourceState);
            writer.WritePackedInt(JointIndex);
            writer.WriteString(PlotID);
            writer.WriteVector3(SpawnerPosition);
            writer.WriteFloat(Scale);
        }
        
        public override void Deserialise(PacketReader reader)
        {
            base.Deserialise(reader);
            ProgressTime = reader.ReadDouble();
            ResourceState = reader.ReadPackedEnum<ResourceCycle.State>();
            JointIndex = reader.ReadPackedInt();
            PlotID = reader.ReadPooledString()!;
            SpawnerPosition = reader.ReadVector3();
            Scale = reader.ReadFloat();
        }
    }
    
    internal sealed class Sprinkle : ActorBase
    {
        public byte MaterialIndex;
        
        protected override ActorType Type => ActorType.Sprinkle;
        
        public override void Serialise(PacketWriter writer)
        {
            base.Serialise(writer);
            writer.WriteByte(MaterialIndex);
        }
        
        public override void Deserialise(PacketReader reader)
        {
            base.Deserialise(reader);
            MaterialIndex = reader.ReadByte();
        }
    }

    internal class Gadget : ActorBase
    {
        public double ChargeupTime;

        protected override ActorType Type => ActorType.Gadget;

        public override void Serialise(PacketWriter writer)
        {
            base.Serialise(writer);
            writer.WriteDouble(ChargeupTime);
        }

        public override void Deserialise(PacketReader reader)
        {
            base.Deserialise(reader);
            ChargeupTime = reader.ReadDouble();
        }
    }

    internal class LinkedGadget : Gadget
    {
        public long LinkedActorId;
        
        protected override ActorType Type => ActorType.LinkedGadget;
        
        public override void Serialise(PacketWriter writer)
        {
            base.Serialise(writer);
            writer.WritePackedLong(LinkedActorId);
        }
        
        public override void Deserialise(PacketReader reader)
        {
            base.Deserialise(reader);
            LinkedActorId = reader.ReadPackedLong();
        }
    }
    
    internal sealed class LinkedAmmoGadget : LinkedGadget
    {
        public NetworkAmmo Ammo;
        
        protected override ActorType Type => ActorType.LinkedGadgetWithAmmo;
        
        public override void Serialise(PacketWriter writer)
        {
            base.Serialise(writer);
            writer.WriteNetObject(Ammo);
        }
        
        public override void Deserialise(PacketReader reader)
        {
            base.Deserialise(reader);
            Ammo = reader.ReadNetObject<NetworkAmmo>();
        }
    }
    
    internal sealed class DroneStation : Gadget
    {
        public float Charge;
        public DroneType DroneType;
        public bool DroneInStation;
        public DroneTask Task;
        public NetworkAmmo Ammo;
        public string DroneOwnerId = string.Empty;

        protected override ActorType Type => ActorType.DroneStation;

        public override void Serialise(PacketWriter writer)
        {
            base.Serialise(writer);
            writer.WriteFloat(Charge);
            writer.WriteEnum(DroneType);
            writer.WriteBool(DroneInStation);
            writer.WriteNetObject(Task);
            writer.WriteNetObject(Ammo);
            writer.WriteString(DroneOwnerId);
        }
        
        public override void Deserialise(PacketReader reader)
        {
            base.Deserialise(reader);
            Charge = reader.ReadFloat();
            DroneType = reader.ReadEnum<DroneType>();
            DroneInStation = reader.ReadBool();
            Task = reader.ReadNetObject<DroneTask>();
            Ammo = reader.ReadNetObject<NetworkAmmo>();
            DroneOwnerId = reader.ReadPooledString()!;
        }
    }
    
    internal struct DroneTask : INetObject
    {
        public int TargetIdent;
        public DroneTaskTargetType Target;
        public DroneTaskSinkType Sink;
        public DroneTaskSourceType Source;
        
        public void Serialise(PacketWriter writer)
        {
            writer.WriteInt(TargetIdent);
            writer.WriteEnum(Target);
            writer.WriteEnum(Sink);
            writer.WriteEnum(Source);
        }
        
        public void Deserialise(PacketReader reader)
        {
            TargetIdent = reader.ReadInt();
            Target = reader.ReadEnum<DroneTaskTargetType>();
            Sink = reader.ReadEnum<DroneTaskSinkType>();
            Source = reader.ReadEnum<DroneTaskSourceType>();
        }
    }
    
    private sealed class ActorTypeComparer : IEqualityComparer<ActorType>
    {
        public static readonly ActorTypeComparer Instance = new();
        
        public bool Equals(ActorType x, ActorType y) => x == y;
        
        public int GetHashCode(ActorType obj) => (int)obj;
    }
}