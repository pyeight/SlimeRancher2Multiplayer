using SR2MP.Packets.Utils;

namespace SR2MP.Packets.LandPlots;

internal sealed class GardenOwnershipPacket : IPacket
{
    public string GardenID;
    public string ClaimerID;
    public string PreviousOwnerID;

    public PacketType Type => PacketType.GardenOwnership;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.Landplots;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(GardenID);
        writer.WriteString(ClaimerID);
        writer.WriteString(PreviousOwnerID);
    }

    public void Deserialise(PacketReader reader)
    {
        GardenID        = reader.ReadPooledString()!;
        ClaimerID       = reader.ReadPooledString() ?? string.Empty;
        PreviousOwnerID = reader.ReadPooledString() ?? string.Empty;
    }
}