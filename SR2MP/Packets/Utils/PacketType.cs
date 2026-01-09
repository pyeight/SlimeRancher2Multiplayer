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
    PediaUnlock = 35,             // Both Ways                                                                          On World FX Play
    MarketPriceChange = 36,       // Both Ways                                                                          On Plort Market Price Change
    GordoFeed = 37,               // Both Ways                                                                          On Gordo Fed
    GordoBurst = 38,              // Both Ways                                                                          On Gordo Burst
    InitialGordos = 39,           // Server -> Client                                                                   Gordos on Load
    InitialSwitches = 40,         // Server -> Client                                                                   Switches on Load
    SwitchActivate = 41,          // Both Ways                                                                          On Switch Activated
}