using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class InitialGordosPacket : IPacket
{
    public sealed class Gordo : INetObject
    {
        public string Id;
        public int EatenCount;
        public int RequiredEatCount;
        public int GordoType;
        public bool WasSeen;
        // public bool Popped;

        public void Serialise(PacketWriter writer)
        {
            writer.WriteString(Id);
            writer.WritePackedInt(EatenCount);
            writer.WritePackedInt(RequiredEatCount);
            writer.WritePackedInt(GordoType);
            writer.WriteBool(WasSeen);
            // writer.WriteBool(Popped);
        }

        public void Deserialise(PacketReader reader)
        {
            Id = reader.ReadString();
            EatenCount = reader.ReadPackedInt();
            RequiredEatCount = reader.ReadPackedInt();
            GordoType = reader.ReadPackedInt();
            WasSeen = reader.ReadBool();
            // Popped = reader.ReadBool();
        }
    }

    public List<Gordo> Gordos;

    public PacketType Type => PacketType.InitialGordos;
    public PacketReliability Reliability => PacketReliability.ReliableOrdered;

    public void Serialise(PacketWriter writer) => writer.WriteList(Gordos, PacketWriterDels.NetObject<Gordo>.Func);

    public void Deserialise(PacketReader reader) => Gordos = reader.ReadList(PacketReaderDels.NetObject<Gordo>.Func);
}