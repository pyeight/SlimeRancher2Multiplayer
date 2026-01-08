using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class SLandPlotsPacket : IPacket
{
    public sealed class Plot : INetObject
    {
        public string ID { get; set; }
        public LandPlot.Id Type { get; set; }
        public CppCollections.HashSet<LandPlot.Upgrade> Upgrades { get; set; }

        public void Serialise(PacketWriter writer)
        {
            writer.WriteString(ID);
            writer.WriteEnum(Type);
            writer.WriteCppSet(Upgrades, PacketWriterDels.Enum<LandPlot.Upgrade>.Func);
        }

        public void Deserialise(PacketReader reader)
        {
            ID = reader.ReadString();
            Type = reader.ReadEnum<LandPlot.Id>();
            Upgrades = reader.ReadCppSet(PacketReaderDels.Enum<LandPlot.Upgrade>.Func);
        }
    }

    public List<Plot> Plots { get; set; }

    public PacketType Type => PacketType.InitialPlots;

    public void Serialise(PacketWriter writer) => writer.WriteList(Plots, PacketWriterDels.NetObject<Plot>.Func);

    public void Deserialise(PacketReader reader) => Plots = reader.ReadList(PacketReaderDels.NetObject<Plot>.Func);
}