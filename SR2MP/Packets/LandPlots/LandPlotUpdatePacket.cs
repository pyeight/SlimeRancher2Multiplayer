using SR2MP.Packets.Utils;

namespace SR2MP.Packets.LandPlots;

internal abstract class LandPlotUpdatePacket : IPacket
{
    public string PlotID;

    public abstract PacketType Type { get; }
    public PacketReliability Reliability => PacketReliability.Reliable;

    public virtual void Serialise(PacketWriter writer) => writer.WriteString(PlotID);

    public virtual void Deserialise(PacketReader reader) => PlotID = reader.ReadString();
}

internal abstract class LandPlotUpdatePacket<T> : LandPlotUpdatePacket where T : struct, Enum
{
    public T ID;

    public sealed override void Serialise(PacketWriter writer)
    {
        base.Serialise(writer);
        writer.WritePackedEnum(ID);
    }

    public sealed override void Deserialise(PacketReader reader)
    {
        base.Deserialise(reader);
        ID = reader.ReadPackedEnum<T>();
    }
}

internal sealed class LandPlotUpgradePacket : LandPlotUpdatePacket<LandPlot.Upgrade>
{
    public override PacketType Type => PacketType.LandPlotUpgrade;
}

internal sealed class NewLandPlotPacket : LandPlotUpdatePacket<LandPlot.Id>
{
    public override PacketType Type => PacketType.NewLandPlot;
}