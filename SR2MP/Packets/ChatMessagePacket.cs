using SR2MP.Packets.Utils;

namespace SR2MP.Packets;

public sealed class ChatMessagePacket : IPacket
{
    public string Username;
    public string Message;
    public string MessageID;
    public byte MessageType;

    public PacketType Type => PacketType.ChatMessage;
    public PacketReliability Reliability => PacketReliability.ReliableOrdered;

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