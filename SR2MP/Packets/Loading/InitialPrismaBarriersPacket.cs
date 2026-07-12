using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

internal sealed class InitialPrismaBarriersPacket : IPacket
{
    internal sealed class Barrier : INetObject
    {
        public string ID;
        public double ActivationTime;

        public void Serialise(PacketWriter writer)
        {
            writer.WriteString(ID);
            writer.WriteDouble(ActivationTime);
        }

        public void Deserialise(PacketReader reader)
        {
            ID = reader.ReadPooledString()!;
            ActivationTime = reader.ReadDouble();
        }
    }

    public List<Barrier> Barriers;

    public PacketType Type => PacketType.InitialPrismaBarriers;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer) => writer.WriteList(Barriers, PacketWriterDels.NetObject<Barrier>.Writer);

    public void Deserialise(PacketReader reader) => Barriers = reader.ReadList(PacketReaderDels.NetObject<Barrier>.Reader)!;
}
