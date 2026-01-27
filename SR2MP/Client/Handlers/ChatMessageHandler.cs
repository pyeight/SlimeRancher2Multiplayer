using SR2MP.Components.UI;
using SR2MP.Packets;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.ChatMessage)]
public sealed class ChatMessageHandler : BaseClientPacketHandler<ChatMessagePacket>
{
    public ChatMessageHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(ChatMessagePacket packet)
    {
        MultiplayerUI.Instance.RegisterChatMessage(packet.Message, packet.Username, packet.MessageID);
    }
}