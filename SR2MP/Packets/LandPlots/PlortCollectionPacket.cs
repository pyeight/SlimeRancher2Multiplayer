using SR2MP.Packets.Utils;

namespace SR2MP.Packets.LandPlots;

internal sealed class PlortCollectionPacket : IPacket
{
    public string ID;
    public double EndTime;

    public PacketType Type => PacketType.PlortCollection;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(ID);
        writer.WriteDouble(EndTime);
    }

    public void Deserialise(PacketReader reader)
    {
        ID = reader.ReadPooledString()!;
        EndTime = reader.ReadDouble();
    }
}