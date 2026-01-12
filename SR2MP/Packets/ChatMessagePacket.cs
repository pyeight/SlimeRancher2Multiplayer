using SR2MP.Packets.Utils;

namespace SR2MP.Packets;

public sealed class ChatMessagePacket : IPacket
{
    public string PlayerId { get; set; }
    public string Message { get; set; }
    public long Timestamp { get; set; }
    public PacketType Type { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(PlayerId);
        writer.WriteString(Message);
        writer.WriteLong(Timestamp);
    }

    public void Deserialise(PacketReader reader)
    {
        PlayerId = reader.ReadString();
        Message = reader.ReadString();
        Timestamp = reader.ReadLong();
    }
}