using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Drone;

internal sealed class DroneOwnershipPacket : IPacket
{
    public long StationId;
    public string ClaimerID = string.Empty;
    public string PreviousOwnerID = string.Empty;

    public PacketType Type => PacketType.DroneOwnership;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WritePackedLong(StationId);
        writer.WriteString(ClaimerID);
        writer.WriteString(PreviousOwnerID);
    }

    public void Deserialise(PacketReader reader)
    {
        StationId = reader.ReadPackedLong();
        ClaimerID = reader.ReadPooledString()!;
        PreviousOwnerID = reader.ReadPooledString()!;
    }
}
