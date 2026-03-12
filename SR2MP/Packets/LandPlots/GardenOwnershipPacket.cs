using SR2MP.Packets.Utils;

namespace SR2MP.Packets.LandPlots;

public sealed class GardenOwnershipPacket : IPacket
{
    public string GardenID;

    public PacketType Type => PacketType.GardenOwnership;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(GardenID);
    }

    public void Deserialise(PacketReader reader)
    {
        GardenID = reader.ReadString();
    }
}