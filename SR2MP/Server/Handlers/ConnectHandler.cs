using System.Net;
using SR2MP.Server.Managers;
using SR2MP.Packets.S2C;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.Connect)]
public class ConnectHandler : BasePacketHandler
{
    private readonly ClientManager clientManager;

    public ConnectHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager)
    {
        this.clientManager = clientManager;
    }

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
            Type = (byte)PacketType.ConnectAck
        };

        SendToClient(ackPacket, client);

        var joinPacket = new BroadcastPlayerJoinPacket
        {
            Type = (byte)PacketType.PlayerJoin,
            PlayerId = playerId
        };

        BroadcastToAllExcept(joinPacket, senderEndPoint);

        SrLogger.LogMessage($"Player {playerId} successfully connected",
            $"Player {playerId} successfully connected from {senderEndPoint}");
    }
}