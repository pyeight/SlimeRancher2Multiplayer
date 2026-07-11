using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Drone;

internal struct DroneCloudExtractionPacket : IPacket
{
    public long StationId;
    public bool Extracting;
    public int TypeId;
    public int Count;

    public readonly PacketType Type => PacketType.DroneCloudExtraction;
    public readonly PacketReliability Reliability => PacketReliability.Reliable;
    public readonly NetworkChannel Channel => NetworkChannel.WorldState;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WritePackedLong(StationId);
        writer.WriteBool(Extracting);
        writer.WritePackedInt(TypeId);
        writer.WritePackedInt(Count);
    }

    public void Deserialise(PacketReader reader)
    {
        StationId = reader.ReadPackedLong();
        Extracting = reader.ReadBool();
        TypeId = reader.ReadPackedInt();
        Count = reader.ReadPackedInt();
    }
}
