using SR2MP.Components.UI;
using SR2MP.Packets;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;
using System.Net;

namespace SR2MP.Shared.Handlers.Player;

public abstract class BasePlayerLeaveHandler : BasePacketHandler<PlayerLeavePacket>
{
    protected BasePlayerLeaveHandler(bool isServerSide)
        : base(isServerSide) { }

    protected void RemovePlayerData(string playerId)
    {
        playerManager.RemovePlayer(playerId);

        if (playerObjects.TryGetValue(playerId, out var playerObj))
        {
            if (playerObj)
            {
                Object.Destroy(playerObj);
                if (!IsServerSide) SrLogger.LogPacketSize($"Destroyed player object for {playerId}", SrLogTarget.Both);
                else SrLogger.LogMessage($"Destroyed player object for {playerId}", SrLogTarget.Both);
            }
            playerObjects.Remove(playerId);
        }
    }
}

[PacketHandler((byte)PacketType.BroadcastPlayerLeave, HandlerType.Client)]
public sealed class ClientPlayerLeaveHandler : BasePlayerLeaveHandler
{
    public ClientPlayerLeaveHandler(bool isServerSide)
        : base(isServerSide) { }

    protected override bool Handle(PlayerLeavePacket packet, IPEndPoint? _)
    {
        if (playerManager.GetPlayer(packet.PlayerId) == null)
        {
            SrLogger.LogMessage($"Player {packet.PlayerId} doesn't exist (already left?)", SrLogTarget.Both);
            return false;
        }

        RemovePlayerData(packet.PlayerId);
        return true;
    }
}

[PacketHandler((byte)PacketType.PlayerLeave, HandlerType.Server)]
public sealed class ServerPlayerLeaveHandler : BasePlayerLeaveHandler
{
    public ServerPlayerLeaveHandler(bool isServerSide)
        : base(isServerSide) { }

    protected override bool Handle(PlayerLeavePacket packet, IPEndPoint? clientEp)
    {
        var playerId = packet.PlayerId;

        if (playerManager.GetPlayer(playerId) == null)
        {
            SrLogger.LogMessage($"Player {playerId} doesn't exist (already left?)", SrLogTarget.Both);
            return false;
        }

        var clientInfo = $"{clientEp!.Address}:{clientEp.Port}";
        SrLogger.LogMessage($"Player leave request received (PlayerId: {playerId})",
            $"Player leave request from {clientInfo} (PlayerId: {playerId})");

        var leaveUsername = playerManager.GetPlayer(playerId)?.Username ?? "Unknown";

        if (Main.Server.clientManager.RemoveClient(clientInfo))
        {
            RemovePlayerData(playerId);

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

        return false;
    }
}