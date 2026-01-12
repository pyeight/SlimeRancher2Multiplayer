using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Time;

public struct WorldTimePacket : IPacket
{
    public double Time { get; set; }
    public PacketType Type { get; set; }

    public readonly void Serialise(PacketWriter writer) => writer.WriteDouble(Time);

    public void Deserialise(PacketReader reader) => Time = reader.ReadDouble();
}