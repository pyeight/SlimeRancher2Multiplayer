using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Landplot;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.WorldState)]
public sealed class LandPlotUpdatePacket : PacketBase
{
    public bool IsUpgrade { get; set; }
    public string ID { get; set; }
    public LandPlot.Id PlotType { get; set; }
    public LandPlot.Upgrade PlotUpgrade { get; set; }

    public override PacketType Type => PacketType.LandPlotUpdate;

    public override void Serialise(PacketWriter writer)
    {
        writer.WriteString(ID);
        writer.WriteBool(IsUpgrade);

        if (!IsUpgrade)
            writer.WriteEnum(PlotType);
        else
            writer.WriteEnum(PlotUpgrade);
    }

    public override void Deserialise(PacketReader reader)
    {
        ID = reader.ReadString();
        IsUpgrade = reader.ReadBool();

        if (!IsUpgrade)
            PlotType = reader.ReadEnum<LandPlot.Id>();
        else
            PlotUpgrade = reader.ReadEnum<LandPlot.Upgrade>();
    }
}