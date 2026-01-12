using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Landplot;

public sealed class LandPlotUpdatePacket : IPacket
{
    public bool IsUpgrade { get; set; }
    public string ID { get; set; }
    public LandPlot.Id PlotType { get; set; }
    public LandPlot.Upgrade PlotUpgrade { get; set; }

    public PacketType Type => PacketType.LandPlotUpdate;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(ID);
        writer.WriteBool(IsUpgrade);

        if (!IsUpgrade)
            writer.WriteEnum(PlotType);
        else
            writer.WriteEnum(PlotUpgrade);
    }

    public void Deserialise(PacketReader reader)
    {
        ID = reader.ReadString();
        IsUpgrade = reader.ReadBool();

        if (!IsUpgrade)
            PlotType = reader.ReadEnum<LandPlot.Id>();
        else
            PlotUpgrade = reader.ReadEnum<LandPlot.Upgrade>();
    }
}