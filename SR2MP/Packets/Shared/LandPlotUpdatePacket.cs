using Il2Cpp;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared;

public struct LandPlotUpdatePacket : IPacket
{
    public byte Type { get; set; }
    public bool IsUpgrade { get; set; }
    public string ID { get; set; }
    public LandPlot.Id PlotType { get; set; }
    public LandPlot.Upgrade PlotUpgrade { get; set; }

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteString(ID);
        writer.WriteBool(IsUpgrade);
        
        if (!IsUpgrade)
            writer.WriteEnum(PlotType);
        else
            writer.WriteEnum(PlotUpgrade);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        ID = reader.ReadString();
        IsUpgrade = reader.ReadBool();
        
        if (!IsUpgrade)
            PlotType = reader.ReadEnum<LandPlot.Id>();
        else
            PlotUpgrade = reader.ReadEnum<LandPlot.Upgrade>();
    }
}