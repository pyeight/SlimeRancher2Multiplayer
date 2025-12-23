using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared;

public struct WorldTimePacket : IPacket
{
    public byte Type { get; set; }
    public double Time { get; set; }

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteDouble(Time);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        Time = reader.ReadDouble();
    }
}