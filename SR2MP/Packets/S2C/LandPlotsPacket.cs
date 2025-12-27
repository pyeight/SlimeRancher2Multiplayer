using SR2MP.Packets.Utils;

namespace SR2MP.Packets.S2C;

public sealed class LandPlotsPacket : IPacket
{
    public sealed class Plot : IPacket
    {
        public string ID { get; set; }
        public LandPlot.Id Type { get; set; }
        public CppCollections.HashSet<LandPlot.Upgrade> Upgrades { get; set; }

        public void Serialise(PacketWriter writer)
        {
            writer.WriteString(ID);
            writer.WriteEnum(Type);
            writer.WriteCppSet(Upgrades, (writer2, value) => writer2.WriteEnum(value));
        }

        public void Deserialise(PacketReader reader)
        {
            ID = reader.ReadString();
            Type = reader.ReadEnum<LandPlot.Id>();
            Upgrades = reader.ReadCppSet(reader2 => reader2.ReadEnum<LandPlot.Upgrade>());
        }
    }

    public byte Type { get; set; }
    public List<Plot> Plots { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteList(Plots, (writer2, value) => value.Serialise(writer2));
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        Plots = reader.ReadList(reader2 => reader2.ReadPacket<Plot>());
    }
}