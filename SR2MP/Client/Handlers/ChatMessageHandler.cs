using SR2MP.Client.Managers;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.BroadcastChatMessage)]
public class ChatMessageHandler : BaseClientPacketHandler
{
    public ChatMessageHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<BroadcastChatMessagePacket>();

        DateTime messageTime = DateTimeOffset.FromUnixTimeMilliseconds(packet.Timestamp).UtcDateTime;

        SrLogger.LogMessage($"[{packet.PlayerId}]: {packet.Message}",
            $"Chat message from {packet.PlayerId} at {messageTime}: {packet.Message}");

        Client.NotifyChatMessageReceived(packet.PlayerId, packet.Message, messageTime);
    }
}