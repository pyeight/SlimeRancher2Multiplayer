using SR2MP.Shared.Managers;
using SR2MP.Components.Player;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.BroadcastPlayerJoin)]
public class PlayerJoinHandler : BaseClientPacketHandler
{
    public PlayerJoinHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        reader.Skip(1);

        string playerId = reader.ReadString();

        playerManager.AddPlayer(playerId);
        
        if (playerId.Equals(Client.OwnPlayerId))
        {
            SrLogger.LogMessage("Player join request accepted!", SrLogger.LogTarget.Both);
            return;
        }

        SrLogger.LogMessage($"New Player joined! (PlayerId: {playerId})", SrLogger.LogTarget.Both);
        
        var playerObject = Object.Instantiate(playerPrefab).GetComponent<NetworkPlayer>();
        playerObject.gameObject.SetActive(true);
        playerObject.ID = playerId;
        playerObject.gameObject.name = playerId;
        playerObjects.Add(playerId, playerObject.gameObject);
        Object.DontDestroyOnLoad(playerObject);
    }
}