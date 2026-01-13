using System.Net;
using SR2MP.Components.UI;
using SR2MP.Packets;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.ChatMessage)]
public sealed class ChatMessageHandler : BasePacketHandler
{
    public ChatMessageHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager)
    {
    }

    public override void Handle(byte[] data, IPEndPoint clientEp)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<ChatMessagePacket>();

        SrLogger.LogMessage($"Chat message from {packet.PlayerId}: {packet.Message}",
            $"Chat message from {clientEp} ({packet.PlayerId}): {packet.Message}");

        packet.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        MultiplayerUI.Instance.RegisterChatMessage(packet.Message, playerManager.GetPlayer(packet.PlayerId)!.Username, packet.Timestamp);
        
        Main.Server.SendToAllExcept(packet, clientEp);
    }
}