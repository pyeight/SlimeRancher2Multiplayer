using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Economy;
using Il2CppMonomiPark.SlimeRancher.Event;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;
using Il2CppMonomiPark.SlimeRancher.Pedia;
using SR2MP.Packets.Economy;
using SR2MP.Packets.Loading;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.Connect)]
public sealed class ConnectHandler : BasePacketHandler
{
    public ConnectHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, IPEndPoint clientEp)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<ConnectPacket>();

        SrLogger.LogMessage($"Connect request received with PlayerId: {packet.PlayerId}",
            $"Connect request from {clientEp} with PlayerId: {packet.PlayerId}");

        clientManager.AddClient(clientEp, packet.PlayerId);

        var money = SceneContext.Instance.PlayerState.GetCurrency(GameContext.Instance.LookupDirector._currencyList[0]
            .Cast<ICurrency>());
        var rainbowMoney =
            SceneContext.Instance.PlayerState.GetCurrency(GameContext.Instance.LookupDirector._currencyList[1]
                .Cast<ICurrency>());

        var ackPacket = new ConnectAckPacket
        {
            PlayerId = packet.PlayerId,
            OtherPlayers = Array.ConvertAll(playerManager.GetAllPlayers().ToArray(), input => (input.PlayerId, input.Username)),
            Money = money,
            RainbowMoney = rainbowMoney,
            AllowCheats = Main.AllowCheats
        };

        Main.Server.SendToClient(ackPacket, clientEp);

        SendPlotsPacket(clientEp);
        SendActorsPacket(clientEp, PlayerIdGenerator.GetPlayerIDNumber(packet.PlayerId));
        SendUpgradesPacket(clientEp);
        SendPediaPacket(clientEp);
        SendMapPacket(clientEp);
        SendPricesPacket(clientEp);
        SendGordosPacket(clientEp);
        SendSwitchesPacket(clientEp);

        SrLogger.LogMessage($"Player {packet.PlayerId} successfully connected",
            $"Player {packet.PlayerId} successfully connected from {clientEp}");
    }

    private static void SendUpgradesPacket(IPEndPoint client)
    {
        var upgrades = new Dictionary<byte, sbyte>();

        foreach (var upgrade in GameContext.Instance.LookupDirector._upgradeDefinitions.items)
        {
            upgrades.Add((byte)upgrade._uniqueId, (sbyte)SceneContext.Instance.PlayerState._model.upgradeModel.GetUpgradeLevel(upgrade));
        }

        var upgradesPacket = new UpgradesPacket
        {
            Upgrades = upgrades,
        };
        Main.Server.SendToClient(upgradesPacket, client);
    }

    private static void SendPediaPacket(IPEndPoint client)
    {
        var unlocked = SceneContext.Instance.PediaDirector._pediaModel.unlocked;

        var unlockedArray = Il2CppSystem.Linq.Enumerable
            .ToArray(unlocked.Cast<CppCollections.IEnumerable<PediaEntry>>());

        var unlockedIDs = unlockedArray.Select(entry => entry.PersistenceId).ToList();

        var pediasPacket = new PediasPacket
        {
            Entries = unlockedIDs
        };

        Main.Server.SendToClient(pediasPacket, client);
    }

    private static void SendMapPacket(IPEndPoint client)
    {
        if (!SceneContext.Instance.eventDirector._model.table.TryGetValue(MapEventKey, out var maps))
        {
            maps = new CppCollections.Dictionary<string, EventRecordModel.Entry>();
            SceneContext.Instance.eventDirector._model.table[MapEventKey] = maps;
        };

        var mapsList = new List<string>();
        
        foreach (var map in maps)
            mapsList.Add(map.Key);

        var mapPacket = new MapPacket()
        {
            UnlockedNodes = mapsList
        };

        Main.Server.SendToClient(mapPacket, client);
    }

    private static void SendActorsPacket(IPEndPoint client, ushort playerIndex)
    {
        var actorsList = new List<ActorsPacket.Actor>();

        foreach (var actorKeyValuePair in SceneContext.Instance.GameModel.identifiables)
        {
            var actor = actorKeyValuePair.Value;
            var model = actor.TryCast<ActorModel>();
            var rotation = model?.lastRotation ?? Quaternion.identity;
            var id = actor.actorId.Value;
            actorsList.Add(new ActorsPacket.Actor
            {
                ActorId = id,
                ActorType = NetworkActorManager.GetPersistentID(actor.ident),
                Position = actor.lastPosition,
                Rotation = rotation,
                Scene = NetworkSceneManager.GetPersistentID(actor.sceneGroup)
            });
        }

        var actorsPacket = new ActorsPacket
        {
            StartingActorID = (uint)NetworkActorManager.GetHighestActorIdInRange(playerIndex * 10000, (playerIndex * 10000) + 10000),
            Actors = actorsList
        };

        Main.Server.SendToClient(actorsPacket, client);
    }

    private static void SendSwitchesPacket(IPEndPoint client)
    {
        var switchesList = new List<SwitchesPacket.Switch>();

        foreach (var switchKeyValuePair in SceneContext.Instance.GameModel.switches)
        {
            switchesList.Add(new SwitchesPacket.Switch
            {
                ID = switchKeyValuePair.key,
                State = switchKeyValuePair.value.state,
            });
        }

        var switchesPacket = new SwitchesPacket()
        {
            Switches = switchesList
        };

        Main.Server.SendToClient(switchesPacket, client);
    }

    private static void SendGordosPacket(IPEndPoint client)
    {
        var gordosList = new List<GordosPacket.Gordo>();

        foreach (var gordo in SceneContext.Instance.GameModel.gordos)
        {
            var eatCount = gordo.value.GordoEatenCount;
            if (eatCount == -1)
                eatCount = gordo.value.targetCount;

            gordosList.Add(new GordosPacket.Gordo
            {
                Id = gordo.key,
                EatenCount = eatCount,
                RequiredEatCount = gordo.value.targetCount,
                GordoType = NetworkActorManager.GetPersistentID(gordo.value.identifiableType)
                //Popped = gordo.value.GordoEatenCount > gordo.value.gordoEatCount
            });
        }

        var gordosPacket = new GordosPacket
        {
            Gordos = gordosList
        };

        Main.Server.SendToClient(gordosPacket, client);
    }

    private static void SendPlotsPacket(IPEndPoint client)
    {
        var plotsList = new List<LandPlotsPacket.BasePlot>();

        foreach (var plotKeyValuePair in SceneContext.Instance.GameModel.landPlots)
        {
            var plot = plotKeyValuePair.Value;
            var id = plotKeyValuePair.Key;

            INetObject? data = null;
            if (plot.typeId == LandPlot.Id.GARDEN)
            {
                data = new LandPlotsPacket.GardenData() 
                { 
                    Crop = plot.resourceGrowerDefinition == null ? 9 : NetworkActorManager.GetPersistentID(plot.resourceGrowerDefinition?._primaryResourceType)
                };
            }
            else if (plot.typeId == LandPlot.Id.SILO)
            {
                // todo
                data = new LandPlotsPacket.SiloData() { };
            }

            plotsList.Add(new LandPlotsPacket.BasePlot
            {
                ID = id,
                Type = plot.typeId,
                Upgrades = plot.upgrades,
                Data = data
            });
        }

        var plotsPacket = new LandPlotsPacket
        {
            Plots = plotsList
        };

        Main.Server.SendToClient(plotsPacket, client);
    }

    private static void SendPricesPacket(IPEndPoint client)
    {
        var pricesPacket = new MarketPricePacket()
        {
            Prices = MarketPricesArray!
        };

        Main.Server.SendToClient(pricesPacket, client);
    }
}