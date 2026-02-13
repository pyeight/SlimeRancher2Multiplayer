using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Switch;

public sealed class WorldSwitchPacket : IPacket
{
    public string ID { get; set; }
    public SwitchHandler.State State { get; set; }
    public bool Immediate { get; set; }

    public PacketType Type => PacketType.SwitchActivate;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(ID);
        writer.WritePackedEnum(State);
        writer.WriteBool(Immediate);
    }

    public void Deserialise(PacketReader reader)
    {
        ID = reader.ReadString();
        State = reader.ReadPackedEnum<SwitchHandler.State>();
        Immediate = reader.ReadBool();
    }
}