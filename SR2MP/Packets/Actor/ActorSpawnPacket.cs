using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;
using Unity.Mathematics;

namespace SR2MP.Packets.Actor;

internal struct ActorSpawnPacket : IPacket
{
    public ActorId ActorId;
    public Quaternion Rotation;
    public Vector3 Position;

    public float4 Emotions;
    public bool Sleeping;

    public int ActorType;
    public byte SceneGroup;
    
    public SlimeAppearance.AppearanceSaveSet FirstAppearance;
    public SlimeAppearance.AppearanceSaveSet SecondAppearance;
    
    public byte Radiancy;

    public byte MaterialIndex;

    public readonly PacketType Type => PacketType.ActorSpawn;
    public readonly PacketReliability Reliability => PacketReliability.Reliable;
    public readonly NetworkChannel Channel => NetworkChannel.ActorCritical;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WritePackedLong(ActorId.Value);
        writer.WriteVector3(Position);
        writer.WriteQuaternion(Rotation);
        writer.WriteFloat4(Emotions);
        writer.WriteBool(Sleeping);
        writer.WritePackedInt(ActorType);
        writer.WriteByte(SceneGroup);
        writer.WritePackedEnum(FirstAppearance);
        writer.WritePackedEnum(SecondAppearance);
        writer.WriteByte(Radiancy);
        writer.WriteByte(MaterialIndex);
    }

    public void Deserialise(PacketReader reader)
    {
        ActorId = new ActorId(reader.ReadPackedLong());
        Position = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
        Emotions = reader.ReadFloat4();
        Sleeping = reader.ReadBool();
        ActorType = reader.ReadPackedInt();
        SceneGroup = reader.ReadByte();
        FirstAppearance = reader.ReadPackedEnum<SlimeAppearance.AppearanceSaveSet>();
        SecondAppearance = reader.ReadPackedEnum<SlimeAppearance.AppearanceSaveSet>();
        Radiancy = reader.ReadByte();
        MaterialIndex = reader.ReadByte();
    }
}