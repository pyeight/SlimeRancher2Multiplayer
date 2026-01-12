using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class GordosPacket : IPacket
{
    public sealed class Gordo : INetObject
    {
        public string Id { get; set; }

        public int EatenCount { get; set; }
        public int RequiredEatCount { get; set; }
        public int GordoType { get; set; }

        // public bool Popped { get; set; }

        public void Serialise(PacketWriter writer)
        {
            writer.WriteString(Id);
            writer.WriteInt(EatenCount);
            writer.WriteInt(RequiredEatCount);
            writer.WriteInt(GordoType);
            // writer.WriteBool(Popped);
        }

        public void Deserialise(PacketReader reader)
        {
            Id = reader.ReadString();
            EatenCount = reader.ReadInt();
            RequiredEatCount = reader.ReadInt();
            GordoType = reader.ReadInt();
            // Popped = reader.ReadBool();
        }
    }

    public List<Gordo> Gordos { get; set; }

    public PacketType Type => PacketType.InitialGordos;

    public void Serialise(PacketWriter writer) => writer.WriteList(Gordos, PacketWriterDels.NetObject<Gordo>.Func);

    public void Deserialise(PacketReader reader) => Gordos = reader.ReadList(PacketReaderDels.NetObject<Gordo>.Func);
}