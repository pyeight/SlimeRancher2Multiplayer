using System.Net;
using SR2MP.Components.Player;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.PlayerJoin)]
public class PlayerJoinHandler : BasePacketHandler
{
    public PlayerJoinHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, IPEndPoint senderEndPoint)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<PlayerJoinPacket>();


        string playerId = packet.PlayerId;

        string address = $"{senderEndPoint.Address}:{senderEndPoint.Port}";

        SrLogger.LogMessage($"Player join request received (PlayerId: {playerId})",
            $"Player join request from {address} (PlayerId: {playerId})");

        var playerObject = Object.Instantiate(playerPrefab).GetComponent<NetworkPlayer>();
        playerObject.gameObject.SetActive(true);
        playerObject.ID = playerId;
        playerObject.gameObject.name = playerId;
        playerObjects.Add(playerId, playerObject.gameObject);
        Object.DontDestroyOnLoad(playerObject);

        var joinPacket = new PlayerJoinPacket
        {
            Type = (byte)PacketType.BroadcastPlayerJoin,
            PlayerId = playerId,
            PlayerName = packet.PlayerName
        };

        Main.Server.SendToAll(joinPacket);
    }
}