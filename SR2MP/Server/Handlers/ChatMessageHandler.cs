using System.Net;
using SR2MP.Components.UI;
using SR2MP.Packets;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.ChatMessage)]
public sealed class ChatMessageHandler : BasePacketHandler<ChatMessagePacket>
{
    public ChatMessageHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager)
    {
    }

    public override void Handle(ChatMessagePacket packet, IPEndPoint clientEp)
    {
        SrLogger.LogMessage($"Chat message from {packet.Username}: {packet.Message}",
            $"Chat message from {clientEp} ({packet.Username}): {packet.Message}");

        MultiplayerUI.Instance.RegisterChatMessage(packet.Message, packet.Username, packet.MessageID);
        
        Main.Server.SendToAllExcept(packet, clientEp);
    }
}