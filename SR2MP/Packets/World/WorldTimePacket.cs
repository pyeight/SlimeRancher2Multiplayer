using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

public struct WorldTimePacket : IPacket
{
    public double Time;

    public PacketType Type { get; set; }
    public readonly PacketReliability Reliability => PacketReliability.Unreliable;

    public readonly void Serialise(PacketWriter writer) => writer.WriteDouble(Time);

    public void Deserialise(PacketReader reader) => Time = reader.ReadDouble();
}