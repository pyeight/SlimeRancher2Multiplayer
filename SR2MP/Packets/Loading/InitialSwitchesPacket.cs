using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class InitialSwitchesPacket : IPacket
{
    public sealed class Switch : INetObject
    {
        public string ID { get; set; }
        public SwitchHandler.State State { get; set; }

        public void Serialise(PacketWriter writer)
        {
            writer.WriteString(ID);
            writer.WriteEnum(State);
        }

        public void Deserialise(PacketReader reader)
        {
            ID = reader.ReadString();
            State = reader.ReadEnum<SwitchHandler.State>();
        }
    }

    public List<Switch> Switches { get; set; }

    public PacketType Type => PacketType.InitialSwitches;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer) => writer.WriteList(Switches, PacketWriterDels.NetObject<Switch>.Func);

    public void Deserialise(PacketReader reader) => Switches = reader.ReadList(PacketReaderDels.NetObject<Switch>.Func);
}