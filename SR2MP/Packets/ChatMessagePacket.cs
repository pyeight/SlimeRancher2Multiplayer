using SR2MP.Packets.Utils;

namespace SR2MP.Packets;

public sealed class ChatMessagePacket : IPacket
{
    public string Username { get; set; }
    public string Message { get; set; }
    public string MessageID { get; set; }
    public PacketType Type => PacketType.ChatMessage;
    public byte MessageType { get; set; } = 0;
    
    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(Username);
        writer.WriteString(Message);
        writer.WriteString(MessageID);
        writer.WriteByte(MessageType);
    }

    public void Deserialise(PacketReader reader)
    {
        Username = reader.ReadString();
        Message = reader.ReadString();
        MessageID = reader.ReadString();
        MessageType = reader.ReadByte();
    }
}