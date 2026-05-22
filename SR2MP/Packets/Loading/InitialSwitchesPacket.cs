using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

internal sealed class InitialSwitchesPacket : IPacket
{
    internal sealed class Switch : INetObject
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
            ID = reader.ReadPooledString()!;
            State = reader.ReadPackedEnum<SwitchHandler.State>();
        }
    }

    public List<Switch> Switches;

    public PacketType Type => PacketType.InitialSwitches;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer) => writer.WriteList(Switches, PacketWriterDels.NetObject<Switch>.Writer);

    public void Deserialise(PacketReader reader) => Switches = reader.ReadList(PacketReaderDels.NetObject<Switch>.Reader)!;
}