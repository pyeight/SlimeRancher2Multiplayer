using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class InitialSwitchesPacket : IPacket
{
    public sealed class Switch : INetObject
    {
        public string ID;
        public SwitchHandler.State State;

        public void Serialise(PacketWriter writer)
        {
            writer.WriteString(ID);
            writer.WritePackedEnum(State);
        }

        public void Deserialise(PacketReader reader)
        {
            ID = reader.ReadString();
            State = reader.ReadPackedEnum<SwitchHandler.State>();
        }
    }

    public List<Switch> Switches;

    public PacketType Type => PacketType.InitialSwitches;
    public PacketReliability Reliability => PacketReliability.ReliableOrdered;

    public void Serialise(PacketWriter writer) => writer.WriteList(Switches, PacketWriterDels.NetObject<Switch>.Func);

    public void Deserialise(PacketReader reader) => Switches = reader.ReadList(PacketReaderDels.NetObject<Switch>.Func);
}