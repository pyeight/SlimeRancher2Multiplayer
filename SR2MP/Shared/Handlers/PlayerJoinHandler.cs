using System.Net;
using SR2MP.Components.Player;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.PlayerJoin)]
public sealed class PlayerJoinHandler : BaseSharedPacketHandler
{
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