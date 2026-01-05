using System.Net;
using SR2MP.Components.Player;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;
using SR2MP.Shared.Managers;

namespace SR2MP.Shared.Handlers;

[PacketHandler((byte)PacketType.PlayerJoin)]
public sealed class PlayerJoinHandler : BaseSharedPacketHandler
{
    public PlayerJoinHandler(NetworkManager networkManager, ClientManager clientManager) {}
    public PlayerJoinHandler(Client.Client client, RemotePlayerManager playerManager) {}
    public override void Handle(byte[] data, IPEndPoint? clientEp = null)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<PlayerJoinPacket>();

        string playerId = packet.PlayerId;

        SrLogger.LogMessage($"Player join request received (PlayerId: {playerId})", SrLogTarget.Both);

        var playerObject = Object.Instantiate(playerPrefab).GetComponent<NetworkPlayer>();
        playerObject.gameObject.SetActive(true);
        playerObject.ID = playerId;
        playerObject.gameObject.name = playerId;
        playerObjects.Add(playerId, playerObject.gameObject);
        playerManager.AddPlayer(playerId).Username = packet.PlayerName!;
        Object.DontDestroyOnLoad(playerObject);

        var joinPacket = new PlayerJoinPacket
        {
            Type = (byte)PacketType.PlayerJoin,
            PlayerId = playerId,
            PlayerName = packet.PlayerName
        };
        
        if (clientEp != null)
            Main.Server.SendToAll(joinPacket);
    }
}