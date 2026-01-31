using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Player;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.Control)]
public sealed class ChatMessagePacket : PacketBase
{
    public string Username { get; set; }
    public string Message { get; set; }
    public string MessageID { get; set; }
    public override PacketType Type => PacketType.ChatMessage;
    public byte MessageType { get; set; } = 0;
    
    public override void Serialise(PacketWriter writer)
    {
        writer.WriteString(Username);
        writer.WriteString(Message);
        writer.WriteString(MessageID);
        writer.WriteByte(MessageType);
    }

    public override void Deserialise(PacketReader reader)
    {
        Username = reader.ReadString();
        Message = reader.ReadString();
        MessageID = reader.ReadString();
        MessageType = reader.ReadByte();
    }
}
