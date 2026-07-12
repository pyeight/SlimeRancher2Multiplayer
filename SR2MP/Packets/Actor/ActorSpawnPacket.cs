using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;
using Unity.Mathematics;

namespace SR2MP.Packets.Actor;

internal struct ActorSpawnPacket : IPacket
{
    public string OwnerId;
    
    public ActorId ActorId;
    public Quaternion Rotation;
    public Vector3 Position;

    public int ActorType;
    public byte SceneGroup;
    
    public float4 Emotions;
    public bool Sleeping;
    public SlimeAppearance.AppearanceSaveSet FirstAppearance;
    public SlimeAppearance.AppearanceSaveSet SecondAppearance;
    public byte Radiancy;

    public byte MaterialIndex;

    public byte SpawnType;

    public readonly PacketType Type => PacketType.ActorSpawn;
    public readonly PacketReliability Reliability => PacketReliability.Reliable;
    public readonly NetworkChannel Channel => NetworkChannel.ActorCritical;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WritePackedLong(ActorId.Value);
        writer.WriteVector3(Position);
        writer.WriteQuaternion(Rotation);
        writer.WritePackedInt(ActorType);
        writer.WriteByte(SceneGroup);
        writer.WriteByte(SpawnType);

        switch (SpawnType)
        {
            case (byte)ActorSpawnType.Slime:
                writer.WriteFloat4(Emotions);
                writer.WriteBool(Sleeping);
                writer.WritePackedEnum(FirstAppearance);
                writer.WritePackedEnum(SecondAppearance);
                writer.WriteByte(Radiancy);
                break;

            case (byte)ActorSpawnType.Sprinkle:
                writer.WriteByte(MaterialIndex);
                break;
        }

        writer.WriteStringWithoutSize(OwnerId);
    }

    public void Deserialise(PacketReader reader)
    {
        ActorId = new ActorId(reader.ReadPackedLong());
        Position = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
        ActorType = reader.ReadPackedInt();
        SceneGroup = reader.ReadByte();
        SpawnType = reader.ReadByte();

        switch (SpawnType)
        {
            case (byte)ActorSpawnType.Slime:
                Emotions = reader.ReadFloat4();
                Sleeping = reader.ReadBool();
                FirstAppearance = reader.ReadPackedEnum<SlimeAppearance.AppearanceSaveSet>();
                SecondAppearance = reader.ReadPackedEnum<SlimeAppearance.AppearanceSaveSet>();
                Radiancy = reader.ReadByte();
                break;

            case (byte)ActorSpawnType.Sprinkle:
                MaterialIndex = reader.ReadByte();
                break;
        }

        OwnerId = reader.ReadPooledStringOfSize(16)!;
    }
}