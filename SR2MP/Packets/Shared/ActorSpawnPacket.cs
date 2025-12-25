using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared;

public struct ActorSpawnPacket : IPacket
{
    public byte Type { get; set; }
    
    public ActorId ActorId { get; set; }
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public int ActorType { get; set; }
    public byte SceneGroup { get; set; }

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteLong(ActorId.Value);
        writer.WriteVector3(Position);
        writer.WriteQuaternion(Rotation);
        writer.WriteInt(ActorType);
        writer.WriteByte(SceneGroup);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        ActorId = new ActorId(reader.ReadLong());
        Position = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
        ActorType = reader.ReadInt();
        SceneGroup = reader.ReadByte();
    }
}