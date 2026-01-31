using LiteNetLib;
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
using UnityEngine.SceneManagement;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.Connect)]
public sealed class ConnectHandler : BasePacketHandler<ConnectPacket>
{
    public ConnectHandler(ClientManager clientManager)
        : base(clientManager) { }

    public override void Handle(ConnectPacket packet, NetPeer clientPeer)
    {
        SrLogger.LogMessage($"Connect request received with PlayerId: {packet.PlayerId}",
            $"Connect request from {clientPeer} with PlayerId: {packet.PlayerId}");

        clientManager.AddClient(clientPeer, packet.PlayerId);

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

        Main.Server.SendToClient(ackPacket, clientPeer);

        SendPlotsPacket(clientPeer);
        SendActorsPacket(clientPeer, PlayerIdGenerator.GetPlayerIDNumber(packet.PlayerId));
        SendUpgradesPacket(clientPeer);
        SendPediaPacket(clientPeer);
        SendMapPacket(clientPeer);
        SendAccessDoorsPacket(clientPeer);
        SendPricesPacket(clientPeer);
        SendGordosPacket(clientPeer);
        SendSwitchesPacket(clientPeer);

        SrLogger.LogMessage($"Player {packet.PlayerId} successfully connected",
            $"Player {packet.PlayerId} successfully connected from {clientPeer}");
    }

    private static void SendUpgradesPacket(NetPeer clientPeer)
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
        Main.Server.SendToClient(upgradesPacket, clientPeer);
    }

    private static void SendPediaPacket(NetPeer clientPeer)
    {
        var unlocked = SceneContext.Instance.PediaDirector._pediaModel.unlocked;

        var unlockedArray = Il2CppSystem.Linq.Enumerable
            .ToArray(unlocked.Cast<CppCollections.IEnumerable<PediaEntry>>());

        var unlockedIDs = unlockedArray.Select(entry => entry.PersistenceId).ToList();

        var pediasPacket = new InitialPediaPacket
        {
            Entries = unlockedIDs
        };

        Main.Server.SendToClient(pediasPacket, clientPeer);
    }

    private static void SendMapPacket(NetPeer clientPeer)
    {
        if (!SceneContext.Instance.eventDirector._model.table.TryGetValue(MapEventKey, out var maps))
        {
            maps = new CppCollections.Dictionary<string, EventRecordModel.Entry>();
            SceneContext.Instance.eventDirector._model.table[MapEventKey] = maps;
        };

        var mapsList = new List<string>();
        
        foreach (var map in maps)
            mapsList.Add(map.Key);

        var mapPacket = new InitialMapPacket()
        {
            UnlockedNodes = mapsList
        };

        Main.Server.SendToClient(mapPacket, clientPeer);
    }

    private static void SendAccessDoorsPacket(NetPeer clientPeer)
    {
        var doorsList = new List<InitialAccessDoorsPacket.Door>();

        foreach (var door in SceneContext.Instance.GameModel.doors)
        {
            doorsList.Add(new InitialAccessDoorsPacket.Door
            {
                ID = door.Key,
                State = door.Value.state
            });
        }

        var accessDoorsPacket = new InitialAccessDoorsPacket()
        {
            Doors = doorsList
        };

        Main.Server.SendToClient(accessDoorsPacket, clientPeer);
    }

    private static void SendActorsPacket(NetPeer clientPeer, ushort playerIndex)
    {
        var actorsList = new List<InitialActorsPacket.Actor>();

        foreach (var actorKeyValuePair in SceneContext.Instance.GameModel.identifiables)
        {
            var actor = actorKeyValuePair.Value;
            var model = actor.TryCast<ActorModel>();
            var rotation = model?.lastRotation ?? Quaternion.identity;
            var id = actor.actorId.Value;
            actorsList.Add(new InitialActorsPacket.Actor
            {
                ActorId = id,
                ActorType = NetworkActorManager.GetPersistentID(actor.ident),
                Position = actor.lastPosition,
                Rotation = rotation,
                Scene = NetworkSceneManager.GetPersistentID(actor.sceneGroup)
            });
        }

        var actorsPacket = new InitialActorsPacket
        {
            StartingActorID = (uint)NetworkActorManager.GetHighestActorIdInRange(playerIndex * 10000, (playerIndex * 10000) + 10000),
            Actors = actorsList
        };

        Main.Server.SendToClient(actorsPacket, clientPeer);
    }

    private static void SendSwitchesPacket(NetPeer clientPeer)
    {
        var switchesList = new List<InitialSwitchesPacket.Switch>();

        foreach (var switchKeyValuePair in SceneContext.Instance.GameModel.switches)
        {
            switchesList.Add(new InitialSwitchesPacket.Switch
            {
                ID = switchKeyValuePair.key,
                State = switchKeyValuePair.value.state,
            });
        }

        var switchesPacket = new InitialSwitchesPacket()
        {
            Switches = switchesList
        };

        Main.Server.SendToClient(switchesPacket, clientPeer);
    }

    private static void SendGordosPacket(NetPeer clientPeer)
    {
        var gordosList = new List<InitialGordosPacket.Gordo>();

        foreach (var gordo in SceneContext.Instance.GameModel.gordos)
        {
            var eatCount = gordo.value.GordoEatenCount;
            if (eatCount == -1)
                eatCount = gordo.value.targetCount;

            gordosList.Add(new InitialGordosPacket.Gordo
            {
                Id = gordo.key,
                EatenCount = eatCount,
                RequiredEatCount = gordo.value.targetCount,
                GordoType = NetworkActorManager.GetPersistentID(gordo.value.identifiableType),
                WasSeen = gordo.value.GordoSeen
                //Popped = gordo.value.GordoEatenCount > gordo.value.gordoEatCount
            });
        }

        var gordosPacket = new InitialGordosPacket
        {
            Gordos = gordosList
        };

        Main.Server.SendToClient(gordosPacket, clientPeer);
    }

    private static void SendPlotsPacket(NetPeer clientPeer)
    {
        var plotsList = new List<InitialLandPlotsPacket.BasePlot>();

        foreach (var plotKeyValuePair in SceneContext.Instance.GameModel.landPlots)
        {
            var plot = plotKeyValuePair.Value;
            var id = plotKeyValuePair.Key;

            INetObject? data = null;
            if (plot.typeId == LandPlot.Id.GARDEN)
            {
                data = new InitialLandPlotsPacket.GardenData() 
                { 
                    Crop = plot.resourceGrowerDefinition == null ? 9 : NetworkActorManager.GetPersistentID(plot.resourceGrowerDefinition?._primaryResourceType!)
                };
            }
            else if (plot.typeId == LandPlot.Id.SILO)
            {
                // todo
                data = new InitialLandPlotsPacket.SiloData() { };
            }

            plotsList.Add(new InitialLandPlotsPacket.BasePlot
            {
                ID = id,
                Type = plot.typeId,
                Upgrades = plot.upgrades,
                Data = data
            });
        }

        var plotsPacket = new InitialLandPlotsPacket
        {
            Plots = plotsList
        };

        Main.Server.SendToClient(plotsPacket, clientPeer);
    }

    private static void SendPricesPacket(NetPeer clientPeer)
    {
        var pricesPacket = new MarketPricePacket()
        {
            Prices = MarketPricesArray!
        };

        Main.Server.SendToClient(pricesPacket, clientPeer);
    }
}