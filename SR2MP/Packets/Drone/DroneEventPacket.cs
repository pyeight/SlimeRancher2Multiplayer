using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Drone;

internal struct DroneEventPacket : IPacket
{
    internal enum EventKind : byte
    {
        PlortSold = 1,
        ResourceCollected = 2
    }

    public EventKind Kind;
    public long StationId;
    public int Scene;
    public double WorldTime;
    public int TypeId;
    public int Count;

    public readonly PacketType Type => PacketType.DroneEvent;
    public readonly PacketReliability Reliability => PacketReliability.Reliable;
    public readonly NetworkChannel Channel => NetworkChannel.WorldState;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteEnum(Kind);
        writer.WritePackedLong(StationId);
        writer.WritePackedInt(Scene);
        writer.WriteDouble(WorldTime);
        writer.WritePackedInt(TypeId);
        writer.WritePackedInt(Count);
    }

    public void Deserialise(PacketReader reader)
    {
        Kind = reader.ReadEnum<EventKind>();
        StationId = reader.ReadPackedLong();
        Scene = reader.ReadPackedInt();
        WorldTime = reader.ReadDouble();
        TypeId = reader.ReadPackedInt();
        Count = reader.ReadPackedInt();
    }
}
