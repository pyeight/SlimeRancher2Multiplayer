using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class SActorsPacket : IPacket
{
    public struct Actor : IPacket
    {
        public long ActorId { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public int ActorType { get; set; }

        public readonly void Serialise(PacketWriter writer)
        {
            writer.WriteVector3(Position);
            writer.WriteQuaternion(Rotation);
            writer.WriteLong(ActorId);
            writer.WriteInt(ActorType);
        }

        public void Deserialise(PacketReader reader)
        {
            Position = reader.ReadVector3();
            Rotation = reader.ReadQuaternion();
            ActorId = reader.ReadLong();
            ActorType = reader.ReadInt();
        }
    }

    public byte Type { get; set; }

    public uint StartingActorID { get; set; } = 10000;

    public List<Actor> Actors { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteUInt(StartingActorID);
        writer.WriteList(Actors, PacketWriterDels.Packet<Actor>.Func);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        StartingActorID = reader.ReadUInt();
        Actors = reader.ReadList(PacketReaderDels.Packet<Actor>.Func);
    }
}