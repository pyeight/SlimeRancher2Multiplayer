using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Economy;
using Il2CppMonomiPark.SlimeRancher.Event;
using Il2CppMonomiPark.SlimeRancher.Pedia;
using Il2CppMonomiPark.SlimeRancher.Weather;
using MelonLoader;
using SR2MP.Components.UI;
using SR2MP.Packets;
using SR2MP.Packets.Economy;
using SR2MP.Packets.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.World;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Utils;

namespace SR2MP.Shared.Managers;

public class ReSyncManager
{
    private readonly Dictionary<string, DateTime> cooldowns = new();
    private static readonly TimeSpan CooldownDuration = TimeSpan.FromMinutes(2);

    public bool CanResync(IPEndPoint endPoint)
    {
        var key = endPoint.ToString();
        return !cooldowns.TryGetValue(key, out var last) || (DateTime.UtcNow - last) >= CooldownDuration;
    }

    public void MarkResynced(IPEndPoint endPoint)
    {
        cooldowns[endPoint.ToString()] = DateTime.UtcNow;
    }

    public void SynchronizeClient(string playerId, IPEndPoint endPoint)
    {
        var money = SceneContext.Instance.PlayerState.GetCurrency(
            GameContext.Instance.LookupDirector._currencyList[0].Cast<ICurrency>());
        var rainbowMoney = SceneContext.Instance.PlayerState.GetCurrency(
            GameContext.Instance.LookupDirector._currencyList[1].Cast<ICurrency>());

        var ackPacket = new ConnectAckPacket
        {
            initialJoin = false,
            PlayerId = playerId,
            OtherPlayers = Array.ConvertAll(playerManager.GetAllPlayers().ToArray(),
                p => (p.PlayerId, p.Username)),
            Money = money,
            RainbowMoney = rainbowMoney,
            AllowCheats = Main.AllowCheats
        };
        Main.Server.SendToClient(ackPacket, endPoint);

        SendGordoSlimesPacket(endPoint);
        SendSwitchesPacket(endPoint);
        SendPlotsPacket(endPoint);
        SendActorsPacket(endPoint, PlayerIdGenerator.GetPlayerIDNumber(playerId));
        SendWeatherPacket(endPoint);
        SendUpgradesPacket(endPoint);
        SendRefineryPacket(endPoint);
        SendPediaPacket(endPoint);
        SendMapPacket(endPoint);
        SendAccessDoorsPacket(endPoint);
        SendPricesPacket(endPoint);

        SrLogger.LogMessage($"Player {playerId} resynced!", $"Player {playerId} ({endPoint}) resynced!");
    }
    
    public void RequestResync()
    {
        if (Main.Client.IsConnected)
        {
            var resyncPacket = new ResyncRequestPacket();
            Main.Client.SendPacket(resyncPacket);
            MultiplayerUI.Instance.RegisterSystemMessage("Resync requested...", $"SYSTEM_RESYNC_REQUEST_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}", MultiplayerUI.SystemMessageNormal);
        }
    }
    
    public void SynchronizeAll()
    {
        foreach (var client in Main.Server.clientManager.GetAllClients())
        {
            SynchronizeClient(client.PlayerId, client.EndPoint);
        }

        var chatPacket = new ChatMessagePacket
        {
            Username = "SYSTEM",
            Message = "Server issued resync for all players.",
            MessageID = $"SYSTEM_RESYNCALL_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            MessageType = MultiplayerUI.SystemMessageConnect
        };
        Main.Server.SendToAll(chatPacket);
    }

    public void SendCooldownMessage(IPEndPoint endPoint)
    {
        var chatPacket = new ChatMessagePacket
        {
            Username = "SYSTEM",
            Message = "Resync is on cooldown. Please wait before requesting another resync.",
            MessageID = $"SYSTEM_RESYNC_COOLDOWN_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            MessageType = MultiplayerUI.SystemMessageDisconnect
        };
        Main.Server.SendToClient(chatPacket, endPoint);
    }

    public void SendSuccessMessage(IPEndPoint endPoint)
    {
        var chatPacket = new ChatMessagePacket
        {
            Username = "SYSTEM",
            Message = "Your client has been resynced.",
            MessageID = $"SYSTEM_RESYNC_DONE_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            MessageType = MultiplayerUI.SystemMessageConnect
        };
        Main.Server.SendToClient(chatPacket, endPoint);
    }

   private static void SendUpgradesPacket(IPEndPoint client)
    {
        var upgrades = new Dictionary<byte, sbyte>();

        foreach (var upgrade in GameContext.Instance.LookupDirector._upgradeDefinitions.items)
        {
            upgrades.Add((byte)upgrade._uniqueId, (sbyte)SceneContext.Instance.PlayerState._model.upgradeModel.GetUpgradeLevel(upgrade));
        }

        var upgradesPacket = new InitialUpgradesPacket { Upgrades = upgrades };
        Main.Server.SendToClient(upgradesPacket, client);
    }

    private static void SendRefineryPacket(IPEndPoint client)
    {
        var refineryItems = new Dictionary<ushort, ushort>();

        foreach (var item in SceneContext.Instance.GadgetDirector._model._itemCounts)
        {
            var itemId = (ushort)NetworkActorManager.GetPersistentID(item.Key);
            var count = (ushort)item.Value;
            refineryItems.Add(itemId, count);
        }

        var refineryPacket = new InitialRefineryPacket
        {
            Items = refineryItems
        };

        Main.Server.SendToClient(refineryPacket, client);
    }

    private static void SendWeatherPacket(IPEndPoint client)
    {
        var weatherRegistry = Resources.FindObjectsOfTypeAll<WeatherRegistry>().FirstOrDefault();
        if (weatherRegistry == null || weatherRegistry._model == null)
        {
            SrLogger.LogError("WeatherRegistry or model not found!", SrLogTarget.Both);
            return;
        }

        MelonCoroutines.Start(
            WeatherPacket.CreateFromModel(
                weatherRegistry._model,
                PacketType.InitialWeather,
                packet => Main.Server.SendToClient(packet, client)
            )
        );
    }

    private static void SendPediaPacket(IPEndPoint client)
    {
        var unlocked = SceneContext.Instance.PediaDirector._pediaModel.unlocked;

        var unlockedArray = Il2CppSystem.Linq.Enumerable
            .ToArray(unlocked.Cast<CppCollections.IEnumerable<PediaEntry>>());

        var unlockedIDs = unlockedArray.Select(entry => entry.Cast<PediaEntry>().PersistenceId).ToList();

        var pediasPacket = new InitialPediaPacket { Entries = unlockedIDs };
        Main.Server.SendToClient(pediasPacket, client);
    }

    private static void SendMapPacket(IPEndPoint client)
    {
        if (!SceneContext.Instance.eventDirector._model.table.TryGetValue(MapEventKey, out var maps))
        {
            maps = new CppCollections.Dictionary<string, EventRecordModel.Entry>();
            SceneContext.Instance.eventDirector._model.table[MapEventKey] = maps;
        }

        var mapsList = new List<string>();

        foreach (var map in maps)
            mapsList.Add(map.Key);

        var mapPacket = new InitialMapPacket
        {
            UnlockedNodes = mapsList
        };

        Main.Server.SendToClient(mapPacket, client);
    }

    private static void SendAccessDoorsPacket(IPEndPoint client)
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

        var accessDoorsPacket = new InitialAccessDoorsPacket
        {
            Doors = doorsList
        };

        Main.Server.SendToClient(accessDoorsPacket, client);
    }

    private static void SendActorsPacket(IPEndPoint client, ushort playerIndex)
    {
        var actorsList = new List<InitialActorsPacket.ActorBase>();

        foreach (var (_, model) in actorManager.Actors)
        {
            actorsList.Add(NetworkActorManager.CreateInitialActor(model));
        }

        foreach (var model in SceneContext.Instance.GameModel.AllGadgets())
            actorsList.Add(NetworkActorManager.CreateInitialActor(model.Cast<IdentifiableModel>()));
        
        var actorsPacket = new InitialActorsPacket
        {
            StartingActorID = (uint)NetworkActorManager.GetHighestActorIdInRange(playerIndex * 10000, (playerIndex * 10000) + 10000),
            Actors = actorsList,
            WorldTime = SceneContext.Instance.GameModel.world.worldTime
        };

        Main.Server.SendToClient(actorsPacket, client);
    }

    private static void SendSwitchesPacket(IPEndPoint client)
    {
        var switchesList = new List<InitialSwitchesPacket.Switch>();

        foreach (var switchKeyValuePair in SceneContext.Instance.GameModel.switches)
        {
            switchesList.Add(new InitialSwitchesPacket.Switch
            {
                ID = switchKeyValuePair.key,
                State = switchKeyValuePair.value.state
            });
        }

        var switchesPacket = new InitialSwitchesPacket
        {
            Switches = switchesList
        };

        Main.Server.SendToClient(switchesPacket, client);
    }

    private static void SendGordoSlimesPacket(IPEndPoint client)
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
                GordoSlimeType = NetworkActorManager.GetPersistentID(gordo.value.identifiableType),
                WasSeen = gordo.value.GordoSeen
                // Popped = gordo.value.GordoEatenCount > gordo.value.gordoEatCount
            });
        }

        var gordosPacket = new InitialGordosPacket
        {
            GordoSlimes = gordosList
        };

        Main.Server.SendToClient(gordosPacket, client);
    }

    private static void SendPlotsPacket(IPEndPoint client)
    {
        var plotsList = new List<InitialLandPlotsPacket.BasePlot>();

        foreach (var plotKeyValuePair in SceneContext.Instance.GameModel.landPlots)
        {
            var plot = plotKeyValuePair.Value;
            var id = plotKeyValuePair.Key;

            INetObject? data = plot.typeId switch
            {
                LandPlot.Id.GARDEN => new InitialLandPlotsPacket.GardenData
                {
                    Crop = plot.resourceGrowerDefinition == null ? 9 : NetworkActorManager.GetPersistentID(plot.resourceGrowerDefinition?._primaryResourceType!)
                },
                LandPlot.Id.SILO => new InitialLandPlotsPacket.SiloData
                    {},
                _ => null
            };

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

        Main.Server.SendToClient(plotsPacket, client);
    }

    private static void SendPricesPacket(IPEndPoint client)
    {
        var pricesPacket = new MarketPricePacket
        {
            Prices = MarketPricesArray!
        };

        Main.Server.SendToClient(pricesPacket, client);
    }
}
