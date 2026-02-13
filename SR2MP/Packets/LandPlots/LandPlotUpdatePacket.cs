using SR2MP.Packets.Utils;

namespace SR2MP.Packets.LandPlots;

public abstract class LandPlotUpdatePacket : IPacket
{
    public string ID;

    public abstract PacketType Type { get; }
    public PacketReliability Reliability => PacketReliability.Reliable;

    public virtual void Serialise(PacketWriter writer) => writer.WriteString(ID);

    public virtual void Deserialise(PacketReader reader) => ID = reader.ReadString();
}

public sealed class LandPlotUpgradePacket : LandPlotUpdatePacket
{
    public LandPlot.Upgrade PlotUpgrade;

    public override PacketType Type => PacketType.LandPlotUpgrade;

    public override void Serialise(PacketWriter writer)
    {
        base.Serialise(writer);
        writer.WritePackedEnum(PlotUpgrade);
    }

    public override void Deserialise(PacketReader reader)
    {
        base.Deserialise(reader);
        PlotUpgrade = reader.ReadPackedEnum<LandPlot.Upgrade>();
    }
}

public sealed class NewLandPlotPacket : LandPlotUpdatePacket
{
    public LandPlot.Id PlotType;

    public override PacketType Type => PacketType.NewLandPlot;

    public override void Serialise(PacketWriter writer)
    {
        base.Serialise(writer);
        writer.WritePackedEnum(PlotType);
    }

    public override void Deserialise(PacketReader reader)
    {
        base.Deserialise(reader);
        PlotType = reader.ReadPackedEnum<LandPlot.Id>();
    }
}