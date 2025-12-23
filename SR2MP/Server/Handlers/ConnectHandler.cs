using System.Net;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.Connect)]
public class ConnectHandler : BasePacketHandler
{
    public ConnectHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, IPEndPoint senderEndPoint)
    {
        using var reader = new PacketReader(data);
        reader.Skip(1);

        string playerId = reader.ReadString();

        SrLogger.LogMessage($"Connect request received with PlayerId: {playerId}",
            $"Connect request from {senderEndPoint} with PlayerId: {playerId}");

        var client = clientManager.AddClient(senderEndPoint, playerId);

        var ackPacket = new ConnectAckPacket
        {
            Type = (byte)PacketType.ConnectAck,
            PlayerId = playerId,
            OtherPlayers = Array.ConvertAll(playerManager.GetAllPlayers().ToArray(), input => input.PlayerId)
        };

        Main.Server.SendToClient(ackPacket, client);

        var joinPacket = new PlayerJoinPacket
        {
            Type = (byte)PacketType.PlayerJoin,
            PlayerId = playerId,
            PlayerName = Main.Username
        };

        Main.Server.SendToAllExcept(joinPacket, senderEndPoint);

        SrLogger.LogMessage($"Player {playerId} successfully connected",
            $"Player {playerId} successfully connected from {senderEndPoint}");
    }
}