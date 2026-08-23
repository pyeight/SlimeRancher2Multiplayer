using Il2CppMonomiPark.SlimeRancher.Event;
using SR2MP.Packets.Loading;
using SR2MP.Packets.World;

namespace SR2MP.Shared.Managers;

internal enum FilterType
{
    Unfiltered,
    Whitelist,
    Blacklist
}

internal static class NetworkEventManager
{
    internal static void Initialize()
    {
        _ = ProducersByKey;
    }
    
    private const FilterType EventKeyFilter = FilterType.Whitelist;
    private const FilterType DataKeyFilter = FilterType.Unfiltered;

    // Whitelist
    private static readonly HashSet<string> EventKeyWhitelist = new()
    {
        "endGameRewardGranted", // End Game Reward (notification?)
        "objectDiscovered"      // A LOT of unlocked things
    };

    // Blacklist
    private static readonly HashSet<string> EventKeyBlacklist = new()
    {
        MapEventKey,                // already synced
        GordoBurstEventKey,         // already synced
        "puzzleSlotUnlocked",       // already synced
        "teleported",               // already synced
        "jump",                     // no need to sync
        "uiDisplayOpened",          // no need to sync
        "uiDisplayClosed",          // no need to sync
        "timePassed",               // already synced via time sync
        "areaEnter",                // no need to sync (?)
        "areaExit",                 // no need to sync (?)
        "groupshot",                // already synced via actor spawn
        "groupvacced",              // already synced via actor destroy
        "shot",                     // already synced via actor spawn
        "vacced",                   // already synced via actor destroy
        "convoPlayed",              // already synced
        "convoRancherPlayed",       // already synced
        "zoneUnlocked",             // already synced
        "tutorialCompleted",        // no need to sync YET
        "pediaEntryPopupClosed",    // no need to sync
        "ranchExpansionPurchased",  // already synced via AccessDoor
        "plotPurchased",            // already synced via LandPlots
        "plotDemolished",           // already synced via LandPlots
        "cropPlanted",              // already synced via GardenPlant
        "gadgetPlaced",             // already synced via actor spawn
        "gadgetObtained",           // already synced via actor destroy
        "gadgetPickedUpOrStored",   // already synced via refinery sync
        "gadgetBlueprintObtained",  // no need to sync (?)
        "corralled",                // no need to sync (analytics)
        "groupcorralled",           // no need to sync (analytics)
        "objectSuctioned",          // already synced via actor update
        "Gift",                     // not synced YET, NPC relationship
        "selected",                 // no need to sync (gadget menu)
        "modeOpened",               // already synchronized / no need (gadget placement mode)
        "modeClosed",               // already synchronized / no need (gadget placement mode)
        "restocked",                // no need to sync YET (shop restocks)
        "refined"                   // already synced via refinery sync
    };

    private static readonly HashSet<string> DataKeyWhitelist = new();

    private static readonly HashSet<string> DataKeyBlacklist = new();
    
    private static readonly HashSet<string> ObjectDiscoveredWhitelist = new()
    {
        "_f79f91ec-b69a-4443-8f82-af6a187e1edb" // Sanctuary boat unlock (?)
    };

    private static Dictionary<string, StringEventProducer>? producersByKey;

    private static Dictionary<string, StringEventProducer> ProducersByKey
    {
        get
        {
            if (producersByKey != null)
                return producersByKey;

            producersByKey = new Dictionary<string, StringEventProducer>();
            
            foreach (var producer in Resources.FindObjectsOfTypeAll<StringEventProducer>())
            {
                if (!string.IsNullOrEmpty(producer.KeyPrefix))
                    producersByKey[producer.KeyPrefix] = producer;
            }

            return producersByKey;
        }
    }

    internal static bool HasProducer(string eventKey)
        => ProducersByKey.ContainsKey(eventKey);

    private static bool ShouldSyncEventKey(string eventKey) => EventKeyFilter switch
    {
        FilterType.Whitelist => EventKeyWhitelist.Contains(eventKey),
        FilterType.Blacklist => !EventKeyBlacklist.Contains(eventKey),
        _ => true
    };

    private static bool ShouldSyncDataKey(string dataKey) => DataKeyFilter switch
    {
        FilterType.Whitelist => DataKeyWhitelist.Contains(dataKey),
        FilterType.Blacklist => !DataKeyBlacklist.Contains(dataKey),
        _ => true
    };

    internal static bool ShouldSync(string eventKey, string dataKey)
    {
        if (!ShouldSyncEventKey(eventKey) || !ShouldSyncDataKey(dataKey))
            return false;

        if (eventKey == "objectDiscovered")
            return ObjectDiscoveredWhitelist.Contains(dataKey);

        return true;
    }

    private static StringEventProducer? FindProducer(string eventKey)
        => ProducersByKey.TryGetValue(eventKey, out var producer) ? producer : null;

    internal static void SendEventRaised(string eventKey, string dataKey)
        => Main.SendToAllOrServer(new EventRaisedPacket { EventKey = eventKey, DataKey = dataKey });

    internal static void ApplyEventRaised(string eventKey, string dataKey)
    {
        var producer = FindProducer(eventKey);
        if (producer == null) return;

        HandlingPacket = true;
        producer.RaiseEventForData(dataKey);
        HandlingPacket = false;
    }

    private static bool HasRaised(string eventKey, string dataKey)
    {
        var model = sceneContext.eventDirector._model;
        if (model == null) return false;

        return model.table.TryGetValue(eventKey, out var dataEntries) && dataEntries.ContainsKey(dataKey);
    }

    internal static void RaiseLocallyOnce(string eventKey, string dataKey)
    {
        if (string.IsNullOrEmpty(eventKey) || string.IsNullOrEmpty(dataKey)) return;
        if (HasRaised(eventKey, dataKey)) return;

        ApplyEventRaised(eventKey, dataKey);
    }

    internal static List<InitialEventsRaisedPacket.Entry> GetRaisedEvents()
    {
        var entries = new List<InitialEventsRaisedPacket.Entry>();
        var table = SceneContext.Instance.eventDirector._model.table;

        foreach (var producer in ProducersByKey.Values)
        {
            if (!ShouldSyncEventKey(producer.KeyPrefix))
                continue;

            if (!table.TryGetValue(producer.KeyPrefix, out var dataEntries))
                continue;

            foreach (var dataEntry in dataEntries)
            {
                // Unsure if we should not sync all initially once
                if (!ShouldSyncDataKey(dataEntry.Key))
                    continue;

                if (producer.KeyPrefix == "objectDiscovered" && !ObjectDiscoveredWhitelist.Contains(dataEntry.Key))
                    continue;

                entries.Add(new InitialEventsRaisedPacket.Entry
                {
                    EventKey = producer.KeyPrefix,
                    DataKey = dataEntry.Key
                });
            }
        }

        return entries;
    }
}
