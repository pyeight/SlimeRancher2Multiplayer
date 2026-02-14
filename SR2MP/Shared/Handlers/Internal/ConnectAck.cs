using System.Net;
using Il2CppMonomiPark.SlimeRancher.Economy;
using SR2MP.Client;
using SR2MP.Components.Player;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;

namespace SR2MP.Shared.Handlers.Internal;

[PacketHandler((byte)PacketType.ConnectAck, HandlerType.Client)]
public sealed class ConnectAckHandler : BasePacketHandler<ConnectAckPacket>
{
    protected override bool Handle(ConnectAckPacket packet, IPEndPoint? _)
    {
        var joinPacket = new PlayerJoinPacket
        {
            Type = PacketType.PlayerJoin,
            PlayerId = packet.PlayerId,
            PlayerName = Main.Username
        };

        PacketSender.SendPacket(joinPacket);

        SR2MPClient.StartHeartbeat();
        Main.Client.NotifyConnected();

        SrLogger.LogMessage($"Connection acknowledged by server! (PlayerId: {packet.PlayerId})", SrLogTarget.Both);

        SceneContext.Instance.PlayerState._model.SetCurrency(GameContext.Instance.LookupDirector._currencyList[0].Cast<ICurrency>(), packet.Money);
        SceneContext.Instance.PlayerState._model.SetCurrency(GameContext.Instance.LookupDirector._currencyList[1].Cast<ICurrency>(), packet.RainbowMoney);

        cheatsEnabled = packet.AllowCheats;

        foreach (var (id, username) in packet.OtherPlayers)
            SpawnPlayer(id, username);

        return false;
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