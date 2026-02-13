using SR2MP.Packets.Utils;

namespace SR2MP.Packets.LandPlots;

public sealed class LandPlotUpdatePacket : IPacket
{
    public bool IsUpgrade { get; set; }
    public string ID { get; set; }
    public LandPlot.Id PlotType { get; set; }
    public LandPlot.Upgrade PlotUpgrade { get; set; }

    public PacketType Type => PacketType.LandPlotUpdate;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(ID);
        writer.WriteBool(IsUpgrade);

        if (!IsUpgrade)
            writer.WritePackedEnum(PlotType);
        else
            writer.WritePackedEnum(PlotUpgrade);
    }

    public void Deserialise(PacketReader reader)
    {
        ID = reader.ReadString();
        IsUpgrade = reader.ReadBool();

        if (!IsUpgrade)
            PlotType = reader.ReadPackedEnum<LandPlot.Id>();
        else
            PlotUpgrade = reader.ReadPackedEnum<LandPlot.Upgrade>();
    }
}

// public abstract class LandPlotUpdatePacket : IPacket
// {
//     public string ID { get; set; }

//     public PacketType Type => PacketType.LandPlotUpdate;
//     public PacketReliability Reliability => PacketReliability.Reliable;

//     public virtual void Serialise(PacketWriter writer) => writer.WriteString(ID);

//     public virtual void Deserialise(PacketReader reader) => ID = reader.ReadString();
// }

// public sealed class LandPlotUpgradePacket : LandPlotUpdatePacket
// {
//     public LandPlot.Upgrade PlotUpgrade { get; set; }

//     public override void Serialise(PacketWriter writer)
//     {
//         base.Serialise(writer);
//         writer.WritePackedEnum(PlotUpgrade);
//     }

//     public override void Deserialise(PacketReader reader)
//     {
//         base.Deserialise(reader);
//         PlotUpgrade = reader.ReadPackedEnum<LandPlot.Upgrade>();
//     }
// }

// public sealed class NewLandPlotUpdatePacket : LandPlotUpdatePacket
// {
//     public LandPlot.Id PlotType { get; set; }

//     public override void Serialise(PacketWriter writer)
//     {
//         base.Serialise(writer);
//         writer.WritePackedEnum(PlotType);
//     }

//     public override void Deserialise(PacketReader reader)
//     {
//         base.Deserialise(reader);
//         PlotType = reader.ReadPackedEnum<LandPlot.Id>();
//     }
// }