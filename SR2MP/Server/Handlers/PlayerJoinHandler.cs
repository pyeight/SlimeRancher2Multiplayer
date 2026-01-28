using System.Net;
using SR2MP.Components.Player;
using SR2MP.Components.UI;
using SR2MP.Packets;
using SR2MP.Packets.Player;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.PlayerJoin)]
public sealed class PlayerJoinHandler : BasePacketHandler<PlayerJoinPacket>
{
    public PlayerJoinHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(PlayerJoinPacket packet, IPEndPoint clientEp)
    {
        string playerId = packet.PlayerId;

        string address = $"{clientEp.Address}:{clientEp.Port}";

        SrLogger.LogMessage($"Player join request received (PlayerId: {playerId})",
            $"Player join request from {address} (PlayerId: {playerId})");

        var playerObject = Object.Instantiate(playerPrefab).GetComponent<NetworkPlayer>();
        playerObject.gameObject.SetActive(true);
        playerObject.ID = playerId;
        playerObject.gameObject.name = playerId;
        playerObjects.Add(playerId, playerObject.gameObject);
        playerManager.AddPlayer(playerId).Username = packet.PlayerName!;
        Object.DontDestroyOnLoad(playerObject);

        var joinPacket = new PlayerJoinPacket
        {
            Type = PacketType.BroadcastPlayerJoin,
            PlayerId = playerId,
            PlayerName = packet.PlayerName
        };

        var joinChatPacket = new ChatMessagePacket
        {
            Username = "SYSTEM",
            Message = $"{packet.PlayerName} joined the world!",
            MessageID = $"SYSTEM_JOIN_{playerId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            MessageType = MultiplayerUI.SystemMessageConnect
        };
        
        Main.Server.SendToAll(joinPacket);
        Main.Server.SendToAllExcept(joinChatPacket, playerId);
        MultiplayerUI.Instance.RegisterSystemMessage($"{packet.PlayerName} joined the world!", $"SYSTEM_JOIN_HOST_{playerId}_" + Extensions.IdGenerator(), MultiplayerUI.SystemMessageConnect);
    }
}