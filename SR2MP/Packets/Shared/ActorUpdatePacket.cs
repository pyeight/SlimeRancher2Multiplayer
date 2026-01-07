using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;
using Unity.Mathematics;

namespace SR2MP.Packets.Shared;

public struct ActorUpdatePacket : IPacket
{
    public ActorId ActorId { get; set; }

    public float4 Emotions { get; set; }
    public Quaternion Rotation { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }

    public byte Type { get; set; }

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteLong(ActorId.Value);
        writer.WriteVector3(Position);
        writer.WriteQuaternion(Rotation);
        writer.WriteVector3(Velocity);
        writer.WriteFloat4(Emotions);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        ActorId = new ActorId(reader.ReadLong());
        Position = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
        Velocity = reader.ReadVector3();
        Emotions = reader.ReadFloat4();
    }
}