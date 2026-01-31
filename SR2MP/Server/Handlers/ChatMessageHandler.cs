using LiteNetLib;
using SR2MP.Components.UI;
using SR2MP.Packets.Player;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.ChatMessage)]
public sealed class ChatMessageHandler : BasePacketHandler<ChatMessagePacket>
{
    public ChatMessageHandler(ClientManager clientManager)
        : base(clientManager)
    {
    }

    public override void Handle(ChatMessagePacket packet, NetPeer clientPeer)
    {
        SrLogger.LogMessage($"Chat message from {packet.Username}: {packet.Message}",
            $"Chat message from {clientPeer} ({packet.Username}): {packet.Message}");

        if (packet.Username == "SYSTEM")
        {
            MultiplayerUI.Instance.RegisterSystemMessage(packet.Message, packet.MessageID, packet.MessageType);
        }
        else
        {
            MultiplayerUI.Instance.RegisterChatMessage(packet.Message, packet.Username, packet.MessageID);
        }
        
        Main.Server.SendToAllExcept(packet, clientPeer);
    }
}
