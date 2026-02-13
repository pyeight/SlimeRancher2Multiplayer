using System.Net;
using SR2MP.Components.UI;
using SR2MP.Packets;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;
using SR2MP.Server.Handlers.Internal;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers.Player;

[PacketHandler((byte)PacketType.PlayerLeave)]
public sealed class PlayerLeaveHandler : BasePacketHandler<PlayerLeavePacket>
{
    public PlayerLeaveHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(PlayerLeavePacket packet, IPEndPoint clientEp)
    {
        var playerId = packet.PlayerId;

        if (playerManager.GetPlayer(playerId) == null)
        {
            SrLogger.LogMessage($"Player {playerId} doesn't exist (already left?)", SrLogTarget.Both);
            return;
        }

        var clientInfo = $"{clientEp.Address}:{clientEp.Port}";

        SrLogger.LogMessage($"Player leave request received (PlayerId: {playerId})",
            $"Player leave request from {clientInfo} (PlayerId: {playerId})");

        var leaveUsername = playerManager.GetPlayer(playerId)?.Username ?? "Unknown";

        if (clientManager.RemoveClient(clientInfo))
        {
            playerManager.RemovePlayer(playerId);

            if (playerObjects.TryGetValue(playerId, out var playerObject))
            {
                if (playerObject != null)
                {
                    Object.Destroy(playerObject);
                    SrLogger.LogMessage($"Destroyed player object for {playerId}", SrLogTarget.Both);
                }
                playerObjects.Remove(playerId);
            }

            var leavePacket = new PlayerLeavePacket
            {
                Type = PacketType.BroadcastPlayerLeave,
                PlayerId = playerId
            };

            Main.Server.SendToAll(leavePacket);

            SrLogger.LogMessage($"Player {playerId} left the server",
                $"Player {playerId} left from {clientInfo}");

            var leaveChatPacket = new ChatMessagePacket
            {
                Username = "SYSTEM",
                Message = $"{leaveUsername} left the world!",
                MessageID = $"SYSTEM_LEAVE_{playerId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                MessageType = MultiplayerUI.SystemMessageDisconnect
            };

            Main.Server.SendToAll(leaveChatPacket);
            MultiplayerUI.Instance.RegisterSystemMessage($"{leaveUsername} left the world!", $"SYSTEM_LEAVE_HOST_{playerId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}", MultiplayerUI.SystemMessageDisconnect);
        }
        else
        {
            SrLogger.LogWarning($"Player leave request from unknown client (PlayerId: {playerId})",
                $"Player leave request from unknown client: {clientInfo} (PlayerId: {playerId})");
        }
    }
}