using SR2MP.Components.UI;
using SR2MP.Packets;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers.Internal;

[PacketHandler((byte)PacketType.ChatMessage)]
public sealed class ChatMessageHandler : BaseClientPacketHandler<ChatMessagePacket>
{
    public ChatMessageHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(ChatMessagePacket packet)
    {
        if (packet.Username == "SYSTEM")
        {
            MultiplayerUI.Instance.RegisterSystemMessage(packet.Message, packet.MessageID, packet.MessageType);
        }
        else
        {
            MultiplayerUI.Instance.RegisterChatMessage(packet.Message, packet.Username, packet.MessageID);
        }
    }
}