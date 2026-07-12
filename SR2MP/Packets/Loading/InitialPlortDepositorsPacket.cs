using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

internal sealed class InitialPlortDepositorsPacket : IPacket
{
    internal sealed class Depositor : INetObject
    {
        public string ID;
        public int AmountDeposited;

        public void Serialise(PacketWriter writer)
        {
            writer.WriteString(ID);
            writer.WriteInt(AmountDeposited);
        }

        public void Deserialise(PacketReader reader)
        {
            ID = reader.ReadPooledString()!;
            AmountDeposited = reader.ReadInt();
        }
    }

    public List<Depositor> Depositors;

    public PacketType Type => PacketType.InitialPlortDepositors;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer) => writer.WriteList(Depositors, PacketWriterDels.NetObject<Depositor>.Writer);

    public void Deserialise(PacketReader reader) => Depositors = reader.ReadList(PacketReaderDels.NetObject<Depositor>.Reader)!;
}
