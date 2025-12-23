using SR2MP.Client.Managers;
using SR2MP.Shared.Managers;
using SR2MP.Components;
using SR2MP.Components.Player;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.ConnectAck)]
public class ConnectAckHandler : BaseClientPacketHandler
{
    public ConnectAckHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<ConnectAckPacket>();

        var joinPacket = new PlayerJoinPacket
        {
            Type = (byte)PacketType.PlayerJoin,
            PlayerId = packet.PlayerId,
            PlayerName = Main.Username
        };

        SendPacket(joinPacket);

        Client.StartHeartbeat();
        Client.NotifyConnected();

        SrLogger.LogMessage($"Connection acknowledged by server! (PlayerId: {packet.PlayerId})",
            SrLogger.LogTarget.Both);

        foreach (var player in packet.OtherPlayers)
        {
            SpawnPlayer(player);
        }
    }

    private void SpawnPlayer(string id)
    {
        var playerObject = Object.Instantiate(playerPrefab).GetComponent<NetworkPlayer>();
        playerObject.gameObject.SetActive(true);
        playerObject.ID = id;
        playerObject.gameObject.name = id;
        playerObjects.Add(id, playerObject.gameObject);
        playerManager.AddPlayer(id);
        Object.DontDestroyOnLoad(playerObject);
    }
}