using Il2CppMonomiPark.SlimeRancher.Economy;
using SR2MP.Shared.Managers;
using SR2MP.Components.Player;
using SR2MP.Packets.Utils;

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
            SrLogTarget.Both);

        SceneContext.Instance.PlayerState._model.SetCurrency(GameContext.Instance.LookupDirector._currencyList[0].Cast<ICurrency>(), packet.Money);
        SceneContext.Instance.PlayerState._model.SetCurrency(GameContext.Instance.LookupDirector._currencyList[1].Cast<ICurrency>(), packet.RainbowMoney);

        CheatsEnabled = packet.AllowCheats;
        
        foreach (var player in packet.OtherPlayers)
        {
            SpawnPlayer(player.ID, player.Username);
        }
    }

    private static void SpawnPlayer(string id, string name)
    {
        var playerObject = Object.Instantiate(playerPrefab).GetComponent<NetworkPlayer>();
        playerObject.gameObject.SetActive(true);
        playerObject.ID = id;
        playerObject.gameObject.name = id;
        playerObjects.Add(id, playerObject.gameObject);
        playerManager.AddPlayer(id).Username = name;
        Object.DontDestroyOnLoad(playerObject);
    }
}