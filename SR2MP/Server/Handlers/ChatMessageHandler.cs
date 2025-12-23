using System.Net;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.ChatMessage)]
public class ChatMessageHandler : BasePacketHandler
{
    public ChatMessageHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager)
    {
    }

    public override void Handle(byte[] data, IPEndPoint senderEndPoint)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<ChatMessagePacket>();

        SrLogger.LogMessage($"Chat message from {packet.PlayerId}: {packet.Message}",
            $"Chat message from {senderEndPoint} ({packet.PlayerId}): {packet.Message}");

        var broadcastPacket = new BroadcastChatMessagePacket
        {
            Type = (byte)PacketType.BroadcastChatMessage,
            PlayerId = packet.PlayerId,
            Message = packet.Message,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        // Broadcast to self for confirmation (if a GUI will exist later qwq)
        // If necessary, not sure how we should do it
        Main.Server.SendToAll(broadcastPacket);
    }
}