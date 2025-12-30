
using Il2CppMonomiPark.SlimeRancher.Economy;
using SR2MP.Client.Managers;
using SR2MP.Shared.Managers;
using SR2MP.Components;
using SR2MP.Components.Player;
using SR2MP.Packets.Utils;
using SR2MP.Packets.Shared;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.ConnectAck)]
public sealed class ConnectAckHandler : BaseClientPacketHandler
{
    public ConnectAckHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<ConnectAckPacket>();

        Client.PendingJoin = new Client.PendingJoinData
        {
            PlayerId = packet.PlayerId,
            Money = packet.Money,
            RainbowMoney = packet.RainbowMoney,
            OtherPlayers = packet.OtherPlayers.ToList()
        };

        SrLogger.LogMessage($"Connection acknowledged by server! Requesting Save Data... (PlayerId: {packet.PlayerId})", SrLogger.LogTarget.Both);

        SendPacket(new RequestSavePacket());
        // Join flow continues in Main.OnSceneWasLoaded after save load
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