namespace SR2MP.Packets.Utils;

public enum PacketType : byte
{   // Type                       // Hierarchy                                    // Exception                          // Use Case
    None = 0,                     // Both Ways                                                                          Empty packet data, exists for convention purposes and default none value
    Connect = 1,                  // Client -> Server                                                                   Try to connect to Server
    ConnectAck = 2,               // Server -> Client                                                                   Initiate Player Join
    Close = 3,                    // Server -> All Clients                                                              Broadcast Server Close
    PlayerJoin = 4,               // Client -> Server                                                                   Add Player
    BroadcastPlayerJoin = 5,      // Server -> All Clients                        (except client that joins)            Add Player on other Clients
    PlayerLeave = 6,              // Client -> Server                                                                   Remove Player
    BroadcastPlayerLeave = 7,     // Server -> All Clients                        (except client that left)             Remove Player on other Clients
    PlayerUpdate = 8,             // Client -> Server                                                                   Update Player
    ChatMessage = 9,              // Client -> Server                                                                   Chat message
    BroadcastChatMessage = 10,     // Server -> All Clients                                                              Chat message on other Clients
    Heartbeat = 11,               // Client -> Server                                                                   Check if Clients are alive
    HeartbeatAck = 12,            // Server -> Client                                                                   Automatically time the Clients out if the Server crashes
    WorldTime = 13,               // Server -> All Clients                                                              Updates Time
    FastForward = 14,             // Client -> Server                                                                   On Sleep & Death
    BroadcastFastForward = 15,    // Server -> All Clients                                                              On Sleep & Death on other clients
    PlayerFX = 16,                // Both Ways                                                                          On Player FX Play
    MovementSound = 17,           // Both Ways                                                                          On Movement SoundPlay
    CurrencyAdjust = 18,          // Both Ways                                                                          On Plort sell
    ActorDestroy = 19,            // Both Ways                                                                          On Actor Destroy
    ActorSpawn = 20,              // Both Ways                                                                          On Actor Spawn
    ActorUpdate = 21,             // Both Ways                                                                          On Actor Update
    ActorTransfer = 22,           // Both Ways                                                                          On Actor Transfer
    InitialActors = 23,           // Server -> Client                                                                   Actors on Load
    LandPlotUpdate = 24,          // Both Ways                                                                          Land plot updates (upgrade or set)
    InitialPlots = 25,            // Server -> Client                                                                   Plots on Load
    WorldFX = 26,                 // Both Ways                                                                          On World FX Play
    InitialPlayerUpgrades = 27,   // Server -> Client                                                                   Player Upgrades on Load
    PlayerUpgrade = 28,           // Both Ways                                                                          On Upgrade
    InitialPediaEntries = 29,     // Server -> Client                                                                   Pedia Entries on Load
    PediaUnlock = 30,             // Both Ways                                                                          On World FX Play
    MarketPriceChange = 31,       // Both Ways                                                                          On Plort Market Price Change
    GordoFeed = 32,               // Both Ways                                                                          On Gordo Fed
    GordoBurst = 33,              // Both Ways                                                                          On Gordo Burst
    InitialGordos = 34,           // Server -> Client                                                                   Gordos on Load
    InitialSwitches = 35,         // Server -> Client                                                                   Switches on Load
    SwitchActivate = 36,          // Both Ways                                                                          On Switch Activated
    ActorUnload = 37,             // Both Ways                                                                          On Actor unloaded
    GeyserTrigger = 38,           // Both Ways                                                                          On Geyser Fired
    MapUnlock = 39,               // Both Ways                                                                          On Geyser Fired
    InitialMap = 40,              // Server -> Client                                                                   Map on Load
    GardenPlant = 41,             // Both Ways                                                                          On Food Planted
}