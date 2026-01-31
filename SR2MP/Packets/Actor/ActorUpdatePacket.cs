using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;
using Unity.Mathematics;

namespace SR2MP.Packets.Actor;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.Sequenced, channel: SR2MP.Networking.NetChannels.EntityPositions)]
public sealed class ActorUpdatePacket : PacketBase
{
    public ActorId ActorId { get; set; }
    public float4 Emotions { get; set; }
    public Quaternion Rotation { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }

    public override PacketType Type => PacketType.ActorUpdate;

    public override void Serialise(PacketWriter writer)
    {
        writer.WriteLong(ActorId.Value);
        writer.WriteVector3(Position);
        writer.WriteQuaternion(Rotation);
        writer.WriteVector3(Velocity);
        writer.WriteFloat4(Emotions);
    }

    public override void Deserialise(PacketReader reader)
    {
        ActorId = new ActorId(reader.ReadLong());
        Position = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
        Velocity = reader.ReadVector3();
        Emotions = reader.ReadFloat4();
    }
}
