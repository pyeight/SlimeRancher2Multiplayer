using Il2CppMonomiPark.SlimeRancher.Economy;
using SR2MP.Shared.Managers;
using SR2MP.Components.Player;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.ConnectAck)]
public sealed class ConnectAckHandler : BaseClientPacketHandler<ConnectAckPacket>
{
    public ConnectAckHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(ConnectAckPacket packet)
    {
        var joinPacket = new PlayerJoinPacket
        {
            Type = PacketType.PlayerJoin,
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

        cheatsEnabled = packet.AllowCheats;

        foreach (var (id, username) in packet.OtherPlayers)
        {
            SpawnPlayer(id, username);
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