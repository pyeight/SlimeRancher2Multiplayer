using LiteNetLib;
using SR2MP.Components.UI;
using SR2MP.Packets.Player;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.PlayerLeave)]
public sealed class PlayerLeaveHandler : BasePacketHandler<PlayerLeavePacket>
{
    public PlayerLeaveHandler(ClientManager clientManager)
        : base(clientManager) { }

    public override void Handle(PlayerLeavePacket packet, NetPeer clientPeer)
    {
        string playerId = packet.PlayerId;

        string clientInfo = $"{clientPeer.Address}:{clientPeer.Port}";

        SrLogger.LogMessage($"Player leave request received (PlayerId: {playerId})",
            $"Player leave request from {clientInfo} (PlayerId: {playerId})");

        if (clientManager.RemoveClient(clientPeer))
        {
            SrLogger.LogMessage($"Player {playerId} left the server",
                $"Player {playerId} left from {clientInfo}");
        }
        else
        {
            SrLogger.LogWarning($"Player leave request from unknown client (PlayerId: {playerId})",
                $"Player leave request from unknown client: {clientInfo} (PlayerId: {playerId})");
        }
    }
}
