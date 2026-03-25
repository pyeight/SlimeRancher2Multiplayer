using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Internal;

internal struct AckPacket : IPacket
{
    public ushort PacketId;
    public byte OriginalPacketType;

    public readonly PacketType Type => PacketType.ReservedAcknowledge;
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