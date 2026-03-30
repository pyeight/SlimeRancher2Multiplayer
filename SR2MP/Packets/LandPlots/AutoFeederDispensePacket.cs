using SR2MP.Packets.Utils;

namespace SR2MP.Packets.LandPlots;

internal sealed class AutoFeederDispensePacket : IPacket
{
    public string ID;
    public double NextTime;

    public PacketType Type => PacketType.AutoFeederDispense;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(ID);
        writer.WriteDouble(NextTime);
    }

    public void Deserialise(PacketReader reader)
    {
        ID = reader.ReadString()!;
        NextTime = reader.ReadDouble();
    }
}