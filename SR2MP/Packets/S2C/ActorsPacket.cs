using SR2MP.Packets.Utils;

namespace SR2MP.Packets.S2C;

public struct ActorsPacket : IPacket
{
    public struct Actor
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public long ActorId { get; set; }
        public int ActorType { get; set; }
    }

    public byte Type { get; set; }

    public List<Actor> Actors { get; set; }

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        
        writer.WriteInt(Actors.Count);
        foreach (var actor in Actors)
        {
            writer.WriteVector3(actor.Position);
            writer.WriteQuaternion(actor.Rotation);
            writer.WriteLong(actor.ActorId);
            writer.WriteInt(actor.ActorType);
        }
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        
        var actorCount = reader.ReadInt();
        Actors = new List<Actor>(actorCount);

        for (var i = 0; i < actorCount; i++)
        {
            Actors.Add(new Actor()
            {
                Position = reader.ReadVector3(),
                Rotation = reader.ReadQuaternion(),
                ActorId = reader.ReadLong(),
                ActorType = reader.ReadInt(),
            });
        }
    }
}