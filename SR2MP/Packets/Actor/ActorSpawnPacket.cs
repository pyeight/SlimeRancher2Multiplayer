using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;
using Unity.Mathematics;

namespace SR2MP.Packets.Actor;

public struct ActorSpawnPacket : IPacket
{
    public ActorId ActorId { get; set; }
    public Quaternion Rotation { get; set; }
    public Vector3 Position { get; set; }
    
    public float4 Emotions { get; set; }
    
    public int ActorType { get; set; }
    public byte SceneGroup { get; set; }

    public readonly PacketType Type => PacketType.ActorSpawn;
    public readonly PacketReliability Reliability => PacketReliability.Reliable;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteLong(ActorId.Value);
        writer.WriteVector3(Position);
        writer.WriteQuaternion(Rotation);
        writer.WriteFloat4(Emotions);
        writer.WriteInt(ActorType);
        writer.WriteByte(SceneGroup);
    }

    public void Deserialise(PacketReader reader)
    {
        ActorId = new ActorId(reader.ReadLong());
        Position = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
        Emotions = reader.ReadFloat4();
        ActorType = reader.ReadInt();
        SceneGroup = reader.ReadByte();
    }
}