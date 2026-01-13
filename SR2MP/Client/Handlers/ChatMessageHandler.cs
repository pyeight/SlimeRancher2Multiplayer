using SR2MP.Components.UI;
using SR2MP.Packets;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.ChatMessage)]
public sealed class ChatMessageHandler : BaseClientPacketHandler
{
    public ChatMessageHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<ChatMessagePacket>();

        DateTime messageTime = DateTimeOffset.FromUnixTimeMilliseconds(packet.Timestamp).UtcDateTime;

        MultiplayerUI.Instance.RegisterChatMessage(packet.Message, playerManager.GetPlayer(packet.PlayerId)!.Username, packet.Timestamp);
    }
}