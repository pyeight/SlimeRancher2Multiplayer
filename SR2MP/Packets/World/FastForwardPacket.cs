using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

internal struct FastForwardPacket : IPacket
{
    public double Time;

    public readonly PacketType Type => PacketType.FastForward;
    public readonly PacketReliability Reliability => PacketReliability.Reliable;
    public readonly NetworkChannel Channel => NetworkChannel.Weather;

    public readonly void Serialise(PacketWriter writer) => writer.WriteDouble(Time);

    public void Deserialise(PacketReader reader) => Time = reader.ReadDouble();
}