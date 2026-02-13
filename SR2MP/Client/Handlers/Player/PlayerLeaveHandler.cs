using SR2MP.Client.Handlers.Internal;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers.Player;

[PacketHandler((byte)PacketType.BroadcastPlayerLeave)]
public sealed class PlayerLeaveHandler : BaseClientPacketHandler<PlayerLeavePacket>
{
    public PlayerLeaveHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(PlayerLeavePacket packet)
    {
        if (playerManager.GetPlayer(packet.PlayerId) == null)
        {
            SrLogger.LogMessage($"Player {packet.PlayerId} doesn't exist (already left?)", SrLogTarget.Both);
            return;
        }

        playerManager.RemovePlayer(packet.PlayerId);

        if (!playerObjects.TryGetValue(packet.PlayerId, out var playerObj))
            return;
        if (playerObj)
        {
            Object.Destroy(playerObj);
            SrLogger.LogPacketSize($"Destroyed player object for {packet.PlayerId}", SrLogTarget.Both);
        }
        playerObjects.Remove(packet.PlayerId);
    }
}