using SR2MP.Packets.Utils;

namespace SR2MP.Packets.S2C;

public struct ConnectAckPacket : IPacket
{
    public byte Type { get; set; }
    public string ServerPlayerId { get; set; }

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
    }
}