using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.WorldState)]
public sealed class InitialSwitchesPacket : PacketBase
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

    public override PacketType Type => PacketType.InitialSwitches;

    public override void Serialise(PacketWriter writer) => writer.WriteList(Switches, PacketWriterDels.NetObject<Switch>.Func);

    public override void Deserialise(PacketReader reader) => Switches = reader.ReadList(PacketReaderDels.NetObject<Switch>.Func);
}
