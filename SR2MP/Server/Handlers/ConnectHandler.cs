using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Economy;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;
using Il2CppMonomiPark.SlimeRancher.Pedia;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.Connect)]
public sealed class ConnectHandler : BasePacketHandler
{
    public ConnectHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, IPEndPoint senderEndPoint)
    {
        using var reader = new PacketReader(data);
        reader.Skip(1);

        var playerId = reader.ReadString();

        SrLogger.LogMessage($"Connect request received with PlayerId: {playerId}",
            $"Connect request from {senderEndPoint} with PlayerId: {playerId}");

        var client = clientManager.AddClient(senderEndPoint, playerId);

        var money = SceneContext.Instance.PlayerState.GetCurrency(GameContext.Instance.LookupDirector._currencyList[0]
            .Cast<ICurrency>());
        var rainbowMoney =
            SceneContext.Instance.PlayerState.GetCurrency(GameContext.Instance.LookupDirector._currencyList[1]
                .Cast<ICurrency>());

        var ackPacket = new ConnectAckPacket
        {
            Type = (byte)PacketType.ConnectAck,
            PlayerId = playerId,
            OtherPlayers = Array.ConvertAll(playerManager.GetAllPlayers().ToArray(), input => input.PlayerId),
            Money = money,
            RainbowMoney = rainbowMoney
        };

        Main.Server.SendToClient(ackPacket, senderEndPoint);

        var joinPacket = new PlayerJoinPacket
        {
            Type = (byte)PacketType.PlayerJoin,
            PlayerId = playerId,
            PlayerName = Main.Username
        };

        Main.Server.SendToAllExcept(joinPacket, senderEndPoint);

        SendPlotsPacket(senderEndPoint);
        SendActorsPacket(senderEndPoint);
        SendUpgradesPacket(senderEndPoint);
        SendPediaPacket(senderEndPoint);

        SrLogger.LogMessage($"Player {playerId} successfully connected",
            $"Player {playerId} successfully connected from {senderEndPoint}");
    }

    private static void SendUpgradesPacket(IPEndPoint client)
    {
        var upgrades = new Dictionary<byte, sbyte>();

        foreach (var upgrade in GameContext.Instance.LookupDirector._upgradeDefinitions.items)
        {
            upgrades.Add((byte)upgrade._uniqueId, (sbyte)SceneContext.Instance.PlayerState._model.upgradeModel.GetUpgradeLevel(upgrade));
        }

        var upgradesPacket = new UpgradesPacket()
        {
            Type = (byte)PacketType.InitialPlayerUpgrades,
            Upgrades = upgrades,
        };
        Main.Server.SendToClient(upgradesPacket, client);
    }

    private static void SendPediaPacket(IPEndPoint client)
    {
        var unlocked = SceneContext.Instance.PediaDirector._pediaModel.unlocked;

        var unlockedArray = Il2CppSystem.Linq.Enumerable
            .ToArray(unlocked.Cast<CppCollections.IEnumerable<PediaEntry>>())
            .ToArray(); // ToArray on an il2cpp array makes it mono

        var unlockedIDs = unlockedArray.Select(entry => entry.PersistenceId);

        var pediasPacket = new PediasPacket()
        {
            Type = (byte)PacketType.InitialPediaEntries,
            Entries = unlockedIDs.ToList()
        };
        Main.Server.SendToClient(pediasPacket, client);
    }

    private static void SendActorsPacket(IPEndPoint client)
    {
        var actorsList = new List<ActorsPacket.Actor>();

        foreach (var actorKeyValuePair in SceneContext.Instance.GameModel.identifiables)
        {
            var actor = actorKeyValuePair.Value;
            var model = actor.TryCast<ActorModel>();
            var rotation = model?.lastRotation ?? Quaternion.identity;
            actorsList.Add(new ActorsPacket.Actor()
            {
                ActorId = actor.actorId.Value,
                ActorType = actorManager.GetPersistentID(actor.ident),
                Position = actor.lastPosition,
                Rotation = rotation,
            });
        }

        var actorsPacket = new ActorsPacket()
        {
            Type = (byte)PacketType.InitialActors,
            Actors = actorsList
        };

        Main.Server.SendToClient(actorsPacket, client);
    }

    private static void SendPlotsPacket(IPEndPoint client)
    {
        var plotsList = new List<LandPlotsPacket.Plot>();

        foreach (var plotKeyValuePair in SceneContext.Instance.GameModel.landPlots)
        {
            var plot = plotKeyValuePair.Value;
            var id = plotKeyValuePair.Key;

            plotsList.Add(new LandPlotsPacket.Plot()
            {
                ID = id,
                Type = plot.typeId,
                Upgrades = plot.upgrades,
            });
        }

        var plotsPacket = new LandPlotsPacket()
        {
            Type = (byte)PacketType.InitialPlots,
            Plots = plotsList
        };

        Main.Server.SendToClient(plotsPacket, client);
    }
}