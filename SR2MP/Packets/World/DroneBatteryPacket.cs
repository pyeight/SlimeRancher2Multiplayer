using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

internal sealed class DroneBatteryPacket : IPacket
{
    public long ActorId;
    public float Charge;
    public float EnergyDepletedPerHour;

    public PacketType Type => PacketType.DroneBattery;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WritePackedLong(ActorId);
        writer.WriteFloat(Charge);
        writer.WriteFloat(EnergyDepletedPerHour);
    }

    public void Deserialise(PacketReader reader)
    {
        ActorId = reader.ReadPackedLong();
        Charge = reader.ReadFloat();
        EnergyDepletedPerHour = reader.ReadFloat();
    }
}
