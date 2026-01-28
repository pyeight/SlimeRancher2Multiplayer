using System.Net;
using SR2MP.Components.UI;
using SR2MP.Packets;
using SR2MP.Packets.Player;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.PlayerLeave)]
public sealed class PlayerLeaveHandler : BasePacketHandler<PlayerLeavePacket>
{
    public PlayerLeaveHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(PlayerLeavePacket packet, IPEndPoint clientEp)
    {
        string playerId = packet.PlayerId;

        string clientInfo = $"{clientEp.Address}:{clientEp.Port}";

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
            int randomComponent = UnityEngine.Random.Range(0, 999999999);
            MultiplayerUI.Instance.RegisterSystemMessage($"{leaveUsername} left the world!", $"SYSTEM_LEAVE_HOST_{playerId}_{randomComponent}", MultiplayerUI.SystemMessageDisconnect);
        }
        else
        {
            SrLogger.LogWarning($"Player leave request from unknown client (PlayerId: {playerId})",
                $"Player leave request from unknown client: {clientInfo} (PlayerId: {playerId})");
        }
    }
}