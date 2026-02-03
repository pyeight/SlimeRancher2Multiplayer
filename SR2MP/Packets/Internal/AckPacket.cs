using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Internal;

public struct AckPacket : IPacket
{
    public ushort PacketId { get; set; }
    public byte OriginalPacketType { get; set; }

    public readonly PacketType Type => PacketType.ReservedAck;
    public readonly PacketReliability Reliability => PacketReliability.Unreliable;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteUShort(PacketId);
        writer.WriteByte(OriginalPacketType);
    }

    public void Deserialise(PacketReader reader)
    {
        PacketId = reader.ReadUShort();
        OriginalPacketType = reader.ReadByte();
    }
}