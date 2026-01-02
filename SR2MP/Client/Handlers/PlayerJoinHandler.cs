using SR2MP.Shared.Managers;
using SR2MP.Components.Player;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.BroadcastPlayerJoin)]
public sealed class PlayerJoinHandler : BaseClientPacketHandler
{
    public PlayerJoinHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<PlayerJoinPacket>();


        if (packet.PlayerId.Equals(Client.OwnPlayerId))
        {
            SrLogger.LogMessage("Player join request accepted!", SrLogTarget.Both);
            return;
        }

        playerManager.AddPlayer(packet.PlayerId).Username = packet.PlayerName!;
        
        SrLogger.LogMessage($"New Player joined! (PlayerId: {packet.PlayerId})", SrLogTarget.Both);

        var playerObject = Object.Instantiate(playerPrefab).GetComponent<NetworkPlayer>();
        playerObject.gameObject.SetActive(true);
        playerObject.ID = packet.PlayerId;
        playerObject.gameObject.name = packet.PlayerId;
        playerObjects.Add(packet.PlayerId, playerObject.gameObject);
        Object.DontDestroyOnLoad(playerObject);
    }
}