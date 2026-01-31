using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Actor;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.WorldState)]
public sealed class ActorSpawnPacket : PacketBase
{
    public ActorId ActorId { get; set; }
    public Quaternion Rotation { get; set; }
    public Vector3 Position { get; set; }
    public int ActorType { get; set; }
    public byte SceneGroup { get; set; }

    public override PacketType Type => PacketType.ActorSpawn;

    public override void Serialise(PacketWriter writer)
    {
        writer.WriteLong(ActorId.Value);
        writer.WriteVector3(Position);
        writer.WriteQuaternion(Rotation);
        writer.WriteInt(ActorType);
        writer.WriteByte(SceneGroup);
    }

    public override void Deserialise(PacketReader reader)
    {
        ActorId = new ActorId(reader.ReadLong());
        Position = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
        ActorType = reader.ReadInt();
        SceneGroup = reader.ReadByte();
    }
}