using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class GordosPacket : IPacket
{
    public struct Gordo : IPacket
    {
        public string Id { get; set; }

        public int EatenCount { get; set; }
        public int RequiredEatCount { get; set; }
        public int GordoType { get; set; }

        //public bool Popped { get; set; }

        public readonly void Serialise(PacketWriter writer)
        {
            writer.WriteString(Id);
            writer.WriteInt(EatenCount);
            writer.WriteInt(RequiredEatCount);
            writer.WriteInt(GordoType);
            //writer.WriteBool(Popped);
        }

        public void Deserialise(PacketReader reader)
        {
            Id = reader.ReadString();
            EatenCount = reader.ReadInt();
            RequiredEatCount = reader.ReadInt();
            GordoType = reader.ReadInt();
            //Popped = reader.ReadBool();
        }
    }

    public byte Type { get; set; }

    public List<Gordo> Gordos { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteList(Gordos, PacketWriterDels.Packet<Gordo>.Func);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        Gordos = reader.ReadList(PacketReaderDels.Packet<Gordo>.Func);
    }
}