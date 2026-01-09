using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class SwitchesPacket : IPacket
{
    public struct Switch : IPacket
    {
        public string ID { get; set; }

        public SwitchHandler.State State { get; set; }

        public readonly void Serialise(PacketWriter writer)
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

    public byte Type { get; set; }

    public List<Switch> Switches { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteList(Switches, PacketWriterDels.Packet<Switch>.Func);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        Switches = reader.ReadList(PacketReaderDels.Packet<Switch>.Func);
    }
}