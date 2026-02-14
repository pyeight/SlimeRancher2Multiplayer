using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

public struct WorldTimePacket : IPacket
{
    public double Time = 0;

    public PacketType Type { get; set; } = PacketType.FastForward;
    public readonly PacketReliability Reliability => PacketReliability.Unreliable;

    public WorldTimePacket() { }

    public readonly void Serialise(PacketWriter writer) => writer.WriteDouble(Time);

    public void Deserialise(PacketReader reader) => Time = reader.ReadDouble();
}