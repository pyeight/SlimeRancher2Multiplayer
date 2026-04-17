using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

internal struct WorldTimePacket : IPacket
{
    public double Time;

    public PacketType Type { get; init; }
    public readonly PacketReliability Reliability => PacketReliability.Unreliable;
    public readonly NetworkChannel Channel => NetworkChannel.Weather;

    public readonly void Serialise(PacketWriter writer) => writer.WriteDouble(Time);

    public void Deserialise(PacketReader reader) => Time = reader.ReadDouble();
}