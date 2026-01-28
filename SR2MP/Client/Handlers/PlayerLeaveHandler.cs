using SR2MP.Shared.Managers;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.BroadcastPlayerLeave)]
public sealed class PlayerLeaveHandler : BaseClientPacketHandler<PlayerLeavePacket>
{
    public PlayerLeaveHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(PlayerLeavePacket packet)
    {
        string playerId = packet.PlayerId;

        SrLogger.LogMessage($"Player left! (PlayerId: {playerId})", SrLogTarget.Both);
        
        var player = playerManager.GetPlayer(playerId);
        if (player != null)
        {
            playerManager.RemovePlayer(playerId);
        }
        
        if (playerObjects.TryGetValue(playerId, out var playerObject))
        {
            if (playerObject != null)
            {
                Object.Destroy(playerObject);
                SrLogger.LogMessage($"Destroyed player object for {playerId}", SrLogTarget.Both);
            }
            playerObjects.Remove(playerId);
        }
    }
}