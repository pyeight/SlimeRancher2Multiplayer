using System.Net;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.PlayerLeave)]
public sealed class PlayerLeaveHandler : BasePacketHandler
{
    public PlayerLeaveHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, string clientIdentifier)
    {
        using var reader = new PacketReader(data);
        reader.Skip(1);

        string playerId = reader.ReadString();

        SrLogger.LogMessage($"Player leave request received (PlayerId: {playerId})",
            $"Player leave request from {clientIdentifier} (PlayerId: {playerId})");

        if (clientManager.RemoveClient(clientIdentifier))
        {
            var leavePacket = new PlayerLeavePacket
            {
                Type = (byte)PacketType.BroadcastPlayerLeave,
                PlayerId = playerId
            };

            Main.Server.SendToAll(leavePacket);

            SrLogger.LogMessage($"Player {playerId} left the server",
                $"Player {playerId} left from {clientIdentifier}");
        }
        else
        {
            SrLogger.LogWarning($"Player leave request from unknown client (PlayerId: {playerId})",
                $"Player leave request from unknown client: {clientIdentifier} (PlayerId: {playerId})");
        }
    }
}