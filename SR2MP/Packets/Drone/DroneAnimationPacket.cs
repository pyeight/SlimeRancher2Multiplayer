using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Drone;

internal struct DroneAnimationPacket : IPacket
{
    public enum DroneAnimation : byte
    {
        Scan = 0,
        Gather = 1,
        Acquisition = 2,
        StopAnimation = 3
    }

    public long StationId;
    public DroneAnimation Animation;

    public readonly PacketType Type => PacketType.DroneAnimation;
    public readonly PacketReliability Reliability => PacketReliability.Reliable;
    public readonly NetworkChannel Channel => NetworkChannel.ActorUpdate;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WritePackedLong(StationId);
        writer.WriteEnum(Animation);
    }

    public void Deserialise(PacketReader reader)
    {
        StationId = reader.ReadPackedLong();
        Animation = reader.ReadEnum<DroneAnimation>();
    }
}
