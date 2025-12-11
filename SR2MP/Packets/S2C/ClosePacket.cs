using SR2MP.Packets.Utils;

namespace SR2MP.Packets.S2C;

public struct ClosePacket : IPacket
{
    public byte Type { get; set; }

    public readonly void Serialise(PacketWriter writer) => writer.WriteByte(Type);

    public void Deserialise(PacketReader reader) => Type = reader.ReadByte();
}