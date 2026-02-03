using SR2MP.Shared.Managers;
using SR2MP.Components.Player;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.BroadcastPlayerJoin)]
public sealed class PlayerJoinHandler : BaseClientPacketHandler<PlayerJoinPacket>
{
    public PlayerJoinHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(PlayerJoinPacket packet)
    {
        if (playerManager.GetPlayer(packet.PlayerId) != null)
        {
            SrLogger.LogPacketSize($"Player {packet.PlayerId} already exists", SrLogTarget.Both);
            return;
        }

        if (packet.PlayerId.Equals(Client.OwnPlayerId))
        {
            SrLogger.LogMessage("Player join request accepted!", SrLogTarget.Both);
            return;
        }
        else
        {
            SrLogger.LogMessage($"New Player joined! (PlayerId: {packet.PlayerId})", SrLogTarget.Both);
        }

        playerManager.AddPlayer(packet.PlayerId).Username = packet.PlayerName!;

        var playerObject = Object.Instantiate(playerPrefab).GetComponent<NetworkPlayer>();
        playerObject.gameObject.SetActive(true);
        playerObject.ID = packet.PlayerId;
        playerObject.gameObject.name = packet.PlayerId;
        playerObjects.Add(packet.PlayerId, playerObject.gameObject);
        Object.DontDestroyOnLoad(playerObject);
    }
}