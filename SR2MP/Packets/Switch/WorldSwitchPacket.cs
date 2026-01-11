using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Switch;

public sealed class WorldSwitchPacket : IPacket
{
    public byte Type { get; set; }

    public string ID { get; set; }
    public SwitchHandler.State State { get; set; }
    public bool Immediate { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteString(ID);
        writer.WriteEnum(State);
        writer.WriteBool(Immediate);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        ID = reader.ReadString();
        State = reader.ReadEnum<SwitchHandler.State>();
        Immediate = reader.ReadBool();
    }
}