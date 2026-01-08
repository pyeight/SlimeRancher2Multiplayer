using SR2MP.Packets.Utils;

namespace SR2MP.Packets;

public sealed class ChatMessagePacket : IPacket
{
    public byte Type { get; set; }
    public string PlayerId { get; set; }
    public string Message { get; set; }
    public long Timestamp { get; set; } = 0;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteString(PlayerId);
        writer.WriteString(Message);
        writer.WriteLong(Timestamp);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        PlayerId = reader.ReadString();
        Message = reader.ReadString();
        Timestamp = reader.ReadLong();
    }
}