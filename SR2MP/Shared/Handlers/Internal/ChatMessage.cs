using System.Net;
using SR2MP.Components.UI;
using SR2MP.Packets;
using SR2MP.Packets.Utils;

namespace SR2MP.Shared.Handlers.Internal;

[PacketHandler((byte)PacketType.ChatMessage)]
public sealed class ChatMessageHandler : BasePacketHandler<ChatMessagePacket>
{
    protected override bool Handle(ChatMessagePacket packet, IPEndPoint? clientEp)
    {
        if (IsServerSide)
        {
            SrLogger.LogMessage($"Chat message from {packet.Username}: {packet.Message}",
                $"Chat message from {clientEp} ({packet.Username}): {packet.Message}");
        }

        if (packet.Username == "SYSTEM")
            MultiplayerUI.Instance.RegisterSystemMessage(packet.Message, packet.MessageID, packet.MessageType);
        else
            MultiplayerUI.Instance.RegisterChatMessage(packet.Message, packet.Username, packet.MessageID);

        return true;
    }
}