using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Economy;
using Il2CppMonomiPark.SlimeRancher.Event;
using Il2CppMonomiPark.SlimeRancher.Pedia;
using Il2CppMonomiPark.SlimeRancher.Weather;
using SR2MP.Components.UI;
using SR2MP.Packets;
using SR2MP.Packets.Ammo;
using SR2MP.Packets.Economy;
using SR2MP.Packets.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Player;
using SR2MP.Packets.TreasurePod;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Server.Managers;
using SR2MP.Shared.Utils;

namespace SR2MP.Shared.Managers;

internal sealed class ReSyncManager
{
    private readonly Dictionary<string, DateTime> cooldowns = new();
    private static readonly TimeSpan CooldownDuration = TimeSpan.FromMinutes(2);

    private DateTime lastSynchronizeAll = DateTime.MinValue;
    private static readonly TimeSpan SynchronizeAllCooldown = TimeSpan.FromSeconds(5);

    public bool CanResync(IPEndPoint endPoint)
    {
        var key = endPoint.ToString();
        return !cooldowns.TryGetValue(key, out var last) || (DateTime.UtcNow - last) >= CooldownDuration;
    }

    public void MarkResynced(IPEndPoint endPoint)
    {
        cooldowns[endPoint.ToString()] = DateTime.UtcNow;
    }

    public static void SynchronizeClient(string playerId, IPEndPoint endPoint)
    {
        var money = SceneContext.Instance.PlayerState.GetCurrency(
            GameContext.Instance.LookupDirector._currencyList[0].Cast<ICurrency>());
        var rainbowMoney = SceneContext.Instance.PlayerState.GetCurrency(
            GameContext.Instance.LookupDirector._currencyList[1].Cast<ICurrency>());

        var approvePacket = new ConnectionApprovePacket
        {
            InitialJoin = false,
            PlayerId = playerId,
            OtherPlayers = Array.ConvertAll(PlayerManager.GetAllPlayers().ToArray(),
                p => (p.PlayerId, p.Username)),
            Money = money,
            RainbowMoney = rainbowMoney,
            AllowCheats = Main.AllowCheats
        };
        Main.Server.SendToClient(approvePacket, endPoint);

        SendPlotsPacket(endPoint);
        SendGordoSlimesPacket(endPoint);
        SendSwitchesPacket(endPoint);
        SendRefineryPacket(endPoint);
        SendPediaPacket(endPoint);
        SendMapPacket(endPoint);
        SendAccessDoorsPacket(endPoint);
        SendResourceNodesPacket(endPoint);
        SendTreasurePodsPacket(endPoint);
        SendUpgradesPacket(endPoint);
        SendActorsPacket(endPoint, PlayerIdGenerator.GetPlayerIDNumber(playerId));
        SendPricesPacket(endPoint);
        SendWeatherPacket(endPoint);
        SendPuzzleSlotsPacket(endPoint);
        SendPlortDepositorsPacket(endPoint);
        SendPrismaBarriersPacket(endPoint);

        SrLogger.LogMessage($"Player {playerId} resynced!", $"Player {playerId} ({endPoint}) resynced!");
    }

    public void RequestResync()
    {
        if (Main.Client.IsConnected)
        {
            var resyncPacket = new ResyncRequestPacket();
            Main.Client.SendPacket(resyncPacket);
            MultiplayerUI.Instance.RegisterSystemMessage("Resync requested...",
                $"SYSTEM_RESYNC_REQUEST_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                MultiplayerUI.SystemMessageNormal);
        }
    }

    public void LogResyncRequest(string playerId, IPEndPoint endPoint)
    {
        SrLogger.LogMessage($"Player {playerId} requested a resync", $"Player {playerId} ({endPoint}) requested a resync");
    }

    public void SynchronizeAll()
    {
        var now = DateTime.UtcNow;
        if ((now - lastSynchronizeAll) < SynchronizeAllCooldown)
        {
            SynchronizeAllCooldownMessage();
            return;
        }
        lastSynchronizeAll = now;

        var clients = Main.Server.ClientManager.GetAllClients().ToList();

        var gordosPacket            = CreateGordoSlimesPacket();
        var switchesPacket          = CreateSwitchesPacket();
        var plotsPacket             = CreatePlotsPacket();
        var upgradesPacket          = CreateUpgradesPacket();
        var refineryPacket          = CreateRefineryPacket();
        var pediaPacket             = CreatePediaPacket();
        var mapPacket               = CreateMapPacket();
        var accessDoorsPacket       = CreateAccessDoorsPacket();
        var resourceNodesPacket     = CreateResourceNodesPacket();
        var treasurePodsPacket      = CreateTreasurePodsPacket();
        var pricesPacket            = CreatePricesPacket();
        var puzzleSlotsPacket       = CreatePuzzleSlotsPacket();
        var plortDepositorsPacket   = CreatePlortDepositorsPacket();
        var prismaBarriersPacket    = CreatePrismaBarriersPacket();

        var money = SceneContext.Instance.PlayerState.GetCurrency(
            GameContext.Instance.LookupDirector._currencyList[0].Cast<ICurrency>());
        var rainbowMoney = SceneContext.Instance.PlayerState.GetCurrency(
            GameContext.Instance.LookupDirector._currencyList[1].Cast<ICurrency>());

        SrLogger.LogMessage("Resyncing all players...");

        foreach (var client in clients)
        {
            var approvePacket = new ConnectionApprovePacket
            {
                InitialJoin = false,
                PlayerId = client.PlayerId,
                OtherPlayers = Array.ConvertAll(PlayerManager.GetAllPlayers().ToArray(),
                    p => (p.PlayerId, p.Username)),
                Money = money,
                RainbowMoney = rainbowMoney,
                AllowCheats = Main.AllowCheats
            };
            Main.Server.SendToClient(approvePacket,         client.EndPoint);

            Main.Server.SendToClient(gordosPacket,          client.EndPoint);
            Main.Server.SendToClient(switchesPacket,        client.EndPoint);
            Main.Server.SendToClient(plotsPacket,           client.EndPoint);
            Main.Server.SendToClient(upgradesPacket,        client.EndPoint);
            Main.Server.SendToClient(refineryPacket,        client.EndPoint);
            Main.Server.SendToClient(pediaPacket,           client.EndPoint);
            Main.Server.SendToClient(mapPacket,             client.EndPoint);
            Main.Server.SendToClient(accessDoorsPacket,     client.EndPoint);
            Main.Server.SendToClient(resourceNodesPacket,   client.EndPoint);
            Main.Server.SendToClient(treasurePodsPacket,    client.EndPoint);
            Main.Server.SendToClient(pricesPacket,          client.EndPoint);
            Main.Server.SendToClient(puzzleSlotsPacket,     client.EndPoint);
            Main.Server.SendToClient(plortDepositorsPacket, client.EndPoint);
            Main.Server.SendToClient(prismaBarriersPacket,  client.EndPoint);

            SendWeatherPacket(client.EndPoint);
            SendActorsPacket(client.EndPoint, PlayerIdGenerator.GetPlayerIDNumber(client.PlayerId));
            SendWeatherPacket(client.EndPoint);
        }

        SrLogger.LogMessage($"Resynced {clients.Count} players!");

        var chatPacket = new ChatMessagePacket
        {
            Username = "SYSTEM",
            Message = "The server issued a resync for all players.",
            MessageID = $"SYSTEM_RESYNC_ALL_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
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

    private static void SynchronizeAllCooldownMessage()
    {
        var chatPacket = new ChatMessagePacket
        {
            Username = "SYSTEM",
            Message = "Resync all is on cooldown. Please wait before resyncing all players.",
            MessageID = $"SYSTEM_RESYNCALL_COOLDOWN_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            MessageType = MultiplayerUI.SystemMessageDisconnect
        };
        MultiplayerUI.Instance.RegisterSystemMessage(chatPacket.Message, chatPacket.MessageID, chatPacket.MessageType);
    }

    private static void SendUpgradesPacket(IPEndPoint client)
        => Main.Server.SendToClient(CreateUpgradesPacket(), client);

    private static InitialUpgradesPacket CreateUpgradesPacket()
    {
        var upgrades = new Dictionary<byte, sbyte>();

        foreach (var upgrade in GameContext.Instance.LookupDirector._upgradeDefinitions.items)
        {
            upgrades.Add((byte)upgrade._uniqueId,
                (sbyte)SceneContext.Instance.PlayerState._model.upgradeModel.GetUpgradeLevel(upgrade));
        }

        return new InitialUpgradesPacket { Upgrades = upgrades };
    }

    private static void SendRefineryPacket(IPEndPoint client)
        => Main.Server.SendToClient(CreateRefineryPacket(), client);

    private static InitialRefineryPacket CreateRefineryPacket()
    {
        var refineryItems = new Dictionary<ushort, ushort>();

        foreach (var item in SceneContext.Instance.GadgetDirector._model._itemCounts)
        {
            var itemId = (ushort)NetworkActorManager.GetPersistentID(item.Key);
            var count = (ushort)item.Value;
            refineryItems.Add(itemId, count);
        }

        return new InitialRefineryPacket { Items = refineryItems };
    }

    private static void SendWeatherPacket(IPEndPoint client)
    {
        var weatherRegistry = Resources.FindObjectsOfTypeAll<WeatherRegistry>().FirstOrDefault();
        if (weatherRegistry == null || weatherRegistry._model == null)
        {
            SrLogger.LogError("WeatherRegistry or model not found!");
            return;
        }

        StartCoroutine(
            WeatherPacket.CreateFromModel(
                weatherRegistry._model,
                PacketType.InitialWeather,
                packet => Main.Server.SendToClient(packet, client)
            )
        );
    }

    private static void SendPediaPacket(IPEndPoint client)
        => Main.Server.SendToClient(CreatePediaPacket(), client);

    private static InitialPediaPacket CreatePediaPacket()
    {
        var unlocked = SceneContext.Instance.PediaDirector._pediaModel.unlocked;

        var unlockedArray = Il2CppSystem.Linq.Enumerable
            .ToArray(unlocked.Cast<CppCollections.IEnumerable<PediaEntry>>());

        var unlockedIDs = unlockedArray.Select(entry => entry.TryCast<PediaEntry>()?.PersistenceId).ToList();

        return new InitialPediaPacket { Entries = unlockedIDs };
    }

    private static void SendMapPacket(IPEndPoint client)
        => Main.Server.SendToClient(CreateMapPacket(), client);

    private static InitialMapPacket CreateMapPacket()
    {
        if (!SceneContext.Instance.eventDirector._model.table.TryGetValue(MapEventKey, out var maps))
        {
            maps = new CppCollections.Dictionary<string, EventRecordModel.Entry>();
            SceneContext.Instance.eventDirector._model.table[MapEventKey] = maps;
        }

        var mapsList = new List<string?>();

        foreach (var map in maps)
            mapsList.Add(map.Key);

        return new InitialMapPacket { UnlockedNodes = mapsList };
    }

    private static void SendAccessDoorsPacket(IPEndPoint client)
        => Main.Server.SendToClient(CreateAccessDoorsPacket(), client);

    private static InitialAccessDoorsPacket CreateAccessDoorsPacket()
    {
        var accessDoorsList = new List<InitialAccessDoorsPacket.Door>();

        foreach (var door in GameState.doors)
        {
            accessDoorsList.Add(new InitialAccessDoorsPacket.Door
            {
                ID = door.Key,
                State = door.Value.state
            });
        }

        return new InitialAccessDoorsPacket { Doors = accessDoorsList };
    }
    
    private static void SendResourceNodesPacket(IPEndPoint client)
        => Main.Server.SendToClient(CreateResourceNodesPacket(), client);
    
    private static InitialResourceNodesPacket CreateResourceNodesPacket()
    {
        var nodesList = new List<ResourceNodePlacement>();

        var spawners = Il2CppSystem.Linq.Enumerable
            .ToArray(GameState.AllResourceNodes()
                .Cast<Il2CppSystem.Collections.Generic.IEnumerable<ResourceNodeSpawnerModel>>());

        foreach (var spawner in spawners)
        {
            if (spawner == null)
                continue;

            nodesList.Add(ResourceNodeManager.CreatePlacement(spawner));
        }

        return new InitialResourceNodesPacket { Nodes = nodesList };
    }

    private static void SendPuzzleSlotsPacket(IPEndPoint client)
        => Main.Server.SendToClient(CreatePuzzleSlotsPacket(), client);

    private static InitialPuzzleSlotsPacket CreatePuzzleSlotsPacket()
    {
        var slotsList = new List<InitialPuzzleSlotsPacket.Slot>();

        foreach (var slot in GameState.slots)
        {
            slotsList.Add(new InitialPuzzleSlotsPacket.Slot
            {
                ID = slot.Key,
                Filled = slot.Value.filled
            });
        }

        return new InitialPuzzleSlotsPacket { Slots = slotsList };
    }

    private static void SendPlortDepositorsPacket(IPEndPoint client)
        => Main.Server.SendToClient(CreatePlortDepositorsPacket(), client);

    private static InitialPlortDepositorsPacket CreatePlortDepositorsPacket()
    {
        var depositorsList = new List<InitialPlortDepositorsPacket.Depositor>();

        foreach (var depositor in GameState.depositors)
        {
            depositorsList.Add(new InitialPlortDepositorsPacket.Depositor
            {
                ID = depositor.Key,
                AmountDeposited = depositor.Value.AmountDeposited
            });
        }

        return new InitialPlortDepositorsPacket { Depositors = depositorsList };
    }

    private static void SendPrismaBarriersPacket(IPEndPoint client)
        => Main.Server.SendToClient(CreatePrismaBarriersPacket(), client);

    private static InitialPrismaBarriersPacket CreatePrismaBarriersPacket()
    {
        var barriersList = new List<InitialPrismaBarriersPacket.Barrier>();

        foreach (var barrier in GameState.AllPrismaBarriers())
        {
            barriersList.Add(new InitialPrismaBarriersPacket.Barrier
            {
                ID = barrier.Key,
                ActivationTime = barrier.Value.ActivationTime
            });
        }

        return new InitialPrismaBarriersPacket { Barriers = barriersList };
    }

    private static void SendActorsPacket(IPEndPoint client, ushort playerIndex)
    {
        var actorsList = new List<InitialActorsPacket.ActorBase>();

        foreach (var (_, model) in ActorManager.Actors)
        {
            if (model.TryCast<GadgetModel>(out _))
                continue;

            actorsList.Add(NetworkActorManager.CreateInitialActor(model));
        }

        foreach (var model in GameState.AllGadgets())
        {
            if (!model.TryCast<GadgetModel>(out var gadget))
                continue;

            actorsList.Add(NetworkActorManager.CreateInitialGadget(gadget));
        }

        var actorsPacket = new InitialActorsPacket
        {
            StartingActorID =
                (uint)NetworkActorManager.GetHighestActorIdInRange(playerIndex * ActorIdOffset,
                    (playerIndex * ActorIdOffset) + ActorIdOffset) + 10,
            Actors = actorsList,
            WorldTime = GameState.world.worldTime
        };

        Main.Server.SendToClient(actorsPacket, client);
    }

    private static void SendSwitchesPacket(IPEndPoint client)
        => Main.Server.SendToClient(CreateSwitchesPacket(), client);

    private static InitialSwitchesPacket CreateSwitchesPacket()
    {
        var switchesList = new List<InitialSwitchesPacket.Switch>();

        foreach (var switchKeyValuePair in GameState.switches)
        {
            switchesList.Add(new InitialSwitchesPacket.Switch
            {
                ID = switchKeyValuePair.key,
                State = switchKeyValuePair.value.state
            });
        }

        return new InitialSwitchesPacket { Switches = switchesList };
    }

    private static void SendGordoSlimesPacket(IPEndPoint client)
        => Main.Server.SendToClient(CreateGordoSlimesPacket(), client);

    private static InitialGordosPacket CreateGordoSlimesPacket()
    {
        var gordoSlimeList = new List<InitialGordosPacket.GordoSlime>();

        foreach (var gordoSlime in GameState.gordos)
        {
            var eatCount = gordoSlime.value.GordoEatenCount;
            if (eatCount == -1)
                eatCount = gordoSlime.value.targetCount;

            gordoSlimeList.Add(new InitialGordosPacket.GordoSlime
            {
                Id = gordoSlime.key,
                EatenCount = eatCount,
                RequiredEatCount = gordoSlime.value.targetCount,
                GordoSlimeType = NetworkActorManager.GetPersistentID(gordoSlime.value.identifiableType),
                WasSeen = gordoSlime.value.GordoSeen
                // Popped = gordoSlime.value.GordoEatenCount > gordoSlime.value.gordoEatCount
            });
        }

        return new InitialGordosPacket { GordoSlimes = gordoSlimeList };
    }

    private static void SendPlotsPacket(IPEndPoint client)
        => Main.Server.SendToClient(CreatePlotsPacket(), client);

    private static InitialLandPlotsPacket CreatePlotsPacket()
    {
        var landPlotsList = new List<InitialLandPlotsPacket.BasePlot>();

        foreach (var (id, plot) in GameState.landPlots)
        {
            INetObject? data = plot.typeId switch
            {
                LandPlot.Id.GARDEN => new InitialLandPlotsPacket.GardenData
                {
                    Crop = plot.resourceGrowerDefinition == null
                        ? 9
                        : NetworkActorManager.GetPersistentID(plot.resourceGrowerDefinition._primaryResourceType!),
                    NextSpawnTime = plot.gameObj?.GetComponentInChildren<SpawnResource>()?._model?.nextSpawnTime ?? 0,
                    StoredWater = plot.gameObj?.GetComponentInChildren<SpawnResource>()?._model?.storedWater ?? 0,
                    NextSpawnRipens = plot.gameObj?.GetComponentInChildren<SpawnResource>()?._model?.nextSpawnRipens ?? false,
                },
                LandPlot.Id.POND => new InitialLandPlotsPacket.CoopPondData
                {
                    CollectorAmmo = SerializeAmmo(plot.siloAmmo, PlortCollectorAmmo)
                },
                LandPlot.Id.COOP => new InitialLandPlotsPacket.CoopPondData
                {
                    CollectorAmmo = SerializeAmmo(plot.siloAmmo, CoopAmmo)
                },
                LandPlot.Id.INCINERATOR => new InitialLandPlotsPacket.IncineratorData
                {
                    AshLevel = plot.ashUnits,
                    PlortCollectorAmmo = SerializeAmmo(plot.siloAmmo, PlortCollectorAmmo)
                },
                LandPlot.Id.SILO => new InitialLandPlotsPacket.SiloData
                {
                    SelectedSlots = plot.siloStorageIndices.ToList().ConvertAll(val => (byte)val),
                    Ammo = SerializeAmmo(plot.siloAmmo, SiloAmmo)
                },
                LandPlot.Id.CORRAL => new InitialLandPlotsPacket.CorralData
                {
                    AutoFeederSpeed = (byte)plot.feederCycleSpeed,
                    PlortCollectorAmmo = SerializeAmmo(plot.siloAmmo, PlortCollectorAmmo),
                    AutoFeederAmmo = SerializeAmmo(plot.siloAmmo, FeederAmmo)
                },
                _ => null
            };

            landPlotsList.Add(new InitialLandPlotsPacket.BasePlot
            {
                ID = id,
                Type = plot.typeId,
                Upgrades = plot.upgrades,
                Data = data
            });
        }

        return new InitialLandPlotsPacket { LandPlots = landPlotsList };
    }

    private static void SendPricesPacket(IPEndPoint client)
        => Main.Server.SendToClient(CreatePricesPacket(), client);

    private static MarketPricePacket CreatePricesPacket()
        => new MarketPricePacket { Prices = MarketPricesArray! };

    private static void SendTreasurePodsPacket(IPEndPoint client)
        => Main.Server.SendToClient(CreateTreasurePodsPacket(), client);

    private static InitialTreasurePodsPacket CreateTreasurePodsPacket()
    {
        var treasurePods = new Dictionary<int, TreasurePod.State>();

        foreach (var treasurePod in GameState.pods)
        {
            if (int.TryParse(treasurePod.key.Replace("pod", string.Empty), out var podId))
                treasurePods.Add(podId, treasurePod.value.state);
        }

        return new InitialTreasurePodsPacket() { TreasurePods = treasurePods };
    }
    
    private static NetworkAmmo SerializeAmmo(
        CppCollections.Dictionary<string, AmmoModel> siloAmmo,
        string ammoKey)
    {
        if (!siloAmmo.ContainsKey(ammoKey))
            return new NetworkAmmo { AmmoSlots = new Dictionary<int, NetworkAmmoSlot>() };

        var slots = siloAmmo[ammoKey].Slots;
        var ammoSlots = new Dictionary<int, NetworkAmmoSlot>();

        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];

            if (slot == null)
                continue;

            if (slot.Definition == null || slot.Definition.name == null)
                continue;

            ammoSlots[i] = new NetworkAmmoSlot
            {
                Count = slot.Count,
                MaxCount = slot.MaxCount,
                Identifiable = NetworkActorManager.GetPersistentID(slot._id),
                SlotDefinition = NetworkAmmoManager.GetId(slot.Definition)
            };
        }

        return new NetworkAmmo { AmmoSlots = ammoSlots };
    }
}