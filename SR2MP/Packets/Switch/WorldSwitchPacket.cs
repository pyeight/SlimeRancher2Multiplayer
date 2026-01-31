using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Switch;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.WorldState)]
public sealed class WorldSwitchPacket : PacketBase
{
    public string ID { get; set; }
    public SwitchHandler.State State { get; set; }
    public bool Immediate { get; set; }

    public override PacketType Type => PacketType.SwitchActivate;

    public override void Serialise(PacketWriter writer)
    {
        writer.WriteString(ID);
        writer.WriteEnum(State);
        writer.WriteBool(Immediate);
    }

    public override void Deserialise(PacketReader reader)
    {
        ID = reader.ReadString();
        State = reader.ReadEnum<SwitchHandler.State>();
        Immediate = reader.ReadBool();
    }
}