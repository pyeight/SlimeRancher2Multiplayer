using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.WorldState)]
public sealed class InitialActorsPacket : PacketBase
{
    public struct Actor : INetObject
    {
        public long ActorId { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public int ActorType { get; set; }
        public int Scene { get; set; }

        public void Serialise(PacketWriter writer)
        {
            writer.WriteVector3(Position);
            writer.WriteQuaternion(Rotation);
            writer.WriteLong(ActorId);
            writer.WriteInt(ActorType);
            writer.WriteInt(Scene);
        }

        public void Deserialise(PacketReader reader)
        {
            Position = reader.ReadVector3();
            Rotation = reader.ReadQuaternion();
            ActorId = reader.ReadLong();
            ActorType = reader.ReadInt();
            Scene = reader.ReadInt();
        }
    }

    public uint StartingActorID { get; set; } = 10000;
    public List<Actor> Actors { get; set; }

    public override PacketType Type => PacketType.InitialActors;

    public override void Serialise(PacketWriter writer)
    {
        writer.WriteUInt(StartingActorID);
        writer.WriteList(Actors, PacketWriterDels.NetObject<Actor>.Func);
    }

    public override void Deserialise(PacketReader reader)
    {
        StartingActorID = reader.ReadUInt();
        Actors = reader.ReadList(PacketReaderDels.NetObject<Actor>.Func);
    }
}
