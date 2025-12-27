using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;
using Unity.Mathematics;

namespace SR2MP.Packets.Shared;

public struct ActorUpdatePacket : IPacket
{
    public byte Type { get; set; }

    public ActorId ActorId { get; set; }
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public Vector3 Velocity { get; set; }
    public float4 Emotions { get; set; }

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteLong(ActorId.Value);
        writer.WriteVector3(Position);
        writer.WriteQuaternion(Rotation);
        writer.WriteVector3(Velocity);

        writer.WriteFloat(Emotions.x);
        writer.WriteFloat(Emotions.y);
        writer.WriteFloat(Emotions.z);
        writer.WriteFloat(Emotions.w);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        ActorId = new ActorId(reader.ReadLong());
        Position = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
        Velocity = reader.ReadVector3();

        var x = reader.ReadFloat();
        var y = reader.ReadFloat();
        var z = reader.ReadFloat();
        var w = reader.ReadFloat();

        Emotions = new float4(x, y, z, w);
    }
}