using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Drone;

internal struct DroneCloudPacket : IPacket
{
    public int TypeId;
    public int Amount;

    public readonly PacketType Type => PacketType.DroneCloud;
    public readonly PacketReliability Reliability => PacketReliability.Reliable;
    public readonly NetworkChannel Channel => NetworkChannel.WorldState;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WritePackedInt(TypeId);
        writer.WritePackedInt(Amount);
    }

    public void Deserialise(PacketReader reader)
    {
        TypeId = reader.ReadPackedInt();
        Amount = reader.ReadPackedInt();
    }
}
