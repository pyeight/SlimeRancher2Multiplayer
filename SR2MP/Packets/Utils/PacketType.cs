namespace SR2MP.Packets.Utils;

public enum PacketType : byte
{   // Type                       // Hierarchy                                    // Exception                          // Use Case
    Connect = 0,                  // Client -> Server                                                                   Try to connect to Server
    ConnectAck = 1,               // Server -> Client                                                                   Initiate Player Join
    Close = 2,                    // Server -> All Clients                                                              Broadcast Server Close
    PlayerJoin = 3,               // Client -> Server                                                                   Add Player
    BroadcastPlayerJoin = 4,      // Server -> All Clients                        (except client that joins)            Add Player on other Clients
    PlayerLeave = 5,              // Client -> Server                                                                   Remove Player
    BroadcastPlayerLeave = 6,     // Server -> All Clients                        (except client that left)             Remove Player on other Clients
    PlayerUpdate = 7,             // Client -> Server                                                                   Update Player
    ChatMessage = 8,              // Client -> Server                                                                   Chat message
    BroadcastChatMessage = 9,     // Server -> All Clients                                                              Chat message on other Clients
    Heartbeat = 16,               // Client -> Server                                                                   Check if Clients are alive
    HeartbeatAck = 17,            // Server -> Client                                                                   Automatically time the Clients out if the Server crashes
    WorldTime = 18,               // Server -> All Clients                                                              Updates Time
    FastForward = 19,             // Client -> Server                                                                   On Sleep & Death
    BroadcastFastForward = 20,    // Server -> All Clients                                                              On Sleep & Death on other clients
    PlayerFX = 21,                // Both Ways                                                                          On Player FX Play
    MovementSound = 22,           // Both Ways                                                                          On Movement SoundPlay
    CurrencyAdjust = 23,          // Both Ways                                                                          On Plort sell
    ActorDestroy = 24,            // Both Ways                                                                          On Actor Destroy
    ActorSpawn = 25,              // Both Ways                                                                          On Actor Spawn
    ActorUpdate = 26,             // Both Ways                                                                          On Actor Update
    ActorTransfer = 27,           // Both Ways                                                                          On Actor Transfer
    InitialActors = 28,           // Server -> Client                                                                   Actors on Load
    LandPlotUpdate = 29,          // Both Ways                                                                          Land plot updates (upgrade or set)
    InitialPlots = 30,            // Server -> Client                                                                   Plots on Load
    WorldFX = 31,                 // Both Ways                                                                          On World FX Play
    InitialPlayerUpgrades = 32,   // Server -> Client                                                                   Player Upgrades on Load
    PlayerUpgrade = 33,           // Both Ways                                                                          On Upgrade
    InitialPediaEntries = 34,     // Server -> Client                                                                   Pedia Entries on Load
    PediaUnlock = 35,             // Both Ways                                                                          On Pedia Unlock
    Inventory = 36,               // Both Ways                                                                          Inventory Slot Update
    Gadget = 37,                  // Both Ways                                                                          Gadget Update
    InitialGadgets = 38,          // Server -> Client                                                                   Gadgets on Load
    Decoration = 39,              // Both Ways                                                                          Decoration Update (Add/Remove)
    PrismaBarrier = 40,           // Both Ways                                                                          Prisma Barrier Activation Time
    PrismaDisruption = 41,        // Both Ways                                                                          Prisma Disruption Level Set
    GardenPlant = 42,             // Both Ways                                                                          Garden Plant Update
    SiloUpdate = 43,              // Both Ways                                                                          Silo Storage Update
    RequestSave = 44,             // Client -> Server                                                                   Request Save Data
    SaveData = 45,                // Server -> Client                                                                   Compressed Save Data
    GordoEat = 46,                // Both Ways                                                                          Gordo Eat Count
    TreasurePod = 47,             // Both Ways                                                                          Treasure Pod Open
    MapUnlock = 48,               // Both Ways                                                                          Map Data Node Unlock
    MarketUpdate = 49,            // Server -> Client                                                                   Market Prices Update
    BlueprintUnlock = 50          // Both Ways                                                                          Gadget Blueprint Unlock
}