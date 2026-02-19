namespace SR2MP.Packets.Utils;

public enum PacketType : byte
{   // Type                       // Hierarchy                                    // Frequency                          // Use Case
    None = 0,                     // Both Ways                                    Unused                                Empty packet data, exists for convention purposes and default none value
    Connect = 1,                  // Client -> Server                             Low (manual)                          Try to connect to Server
    ConnectAck = 2,               // Server -> Client                             Low (depends on connect)              Initiate Player Join
    Close = 3,                    // Server -> All Clients                        Low (manual)                          Broadcast Server Close
    PlayerJoin = 4,               // Client -> Server                             Low (manual)                          Join a World
    BroadcastPlayerJoin = 5,      // Server -> All Clients                        Low (depends on join)                 Add Player on other Clients
    PlayerLeave = 6,              // Client -> Server                             Low (manual)                          Leave a World
    BroadcastPlayerLeave = 7,     // Server -> All Clients                        Low (depends on leave)                Remove Player on other Clients
    PlayerUpdate = 8,             // Client -> Server                             Very High                             Update Player
    ChatMessage = 9,              // Client -> Server                             Low (manual)                          Chat message
    Heartbeat = 10,               // Client -> Server                             Low (Currently unused)                Check if Clients are alive
    HeartbeatAck = 11,            // Server -> Client                             Low (depends on heartbeat)            Automatically time the Clients out if the Server crashes
    WorldTime = 12,               // Server -> All Clients                        Very High (depends on join too)       Updates Time
    FastForward = 13,             // Client -> Server                             Middle (can be manual)                On Sleep & Death
    PlayerFX = 14,                // Both Ways                                    Middle (manual)                       On Player FX
    MovementSound = 15,           // Both Ways                                    Middle (manual)                       On Movement SoundPlay
    CurrencyAdjust = 16,          // Both Ways                                    High (can be manual)                  On Plort sell
    ActorDestroy = 17,            // Both Ways                                    High (can be manual)                  On Actor Destroy
    ActorSpawn = 18,              // Both Ways                                    High (can be manual)                  On Actor Spawn
    ActorUpdate = 19,             // Both Ways                                    Very High                             On Actor Update
    ActorTransfer = 20,           // Both Ways                                    High (can be manual)                  On Actor Transfer
    InitialActors = 21,           // Server -> Client                             Low                                   Actors on Load
    NewLandPlot = 22,             // Both Ways                                    Low (manual)                          New land plot set
    InitialPlots = 23,            // Server -> Client                             Low                                   Plots on Load
    WorldFX = 24,                 // Both Ways                                    High                                  On World FX
    InitialPlayerUpgrades = 25,   // Server -> Client                             Low                                   Player Upgrades on Load
    PlayerUpgrade = 26,           // Both Ways                                    Low (manual)                          On Upgrade
    InitialPediaEntries = 27,     // Server -> Client                             Low                                   Pedia Entries on Load
    PediaUnlock = 28,             // Both Ways                                    Middle (manual)                       On Pedia entry
    MarketPriceChange = 29,       // Both Ways                                    Middle                                On Plort Market Price Change
    GordoFeed = 30,               // Both Ways                                    Middle (manual)                       On Gordo Fed
    GordoBurst = 31,              // Both Ways                                    Low (depends on feed)                 On Gordo Burst
    InitialGordos = 32,           // Server -> Client                             Low (depends on join)                 Gordos on Load
    InitialSwitches = 33,         // Server -> Client                             Low (depends on join)                 Switches on Load
    SwitchActivate = 34,          // Both Ways                                    Low                                   On Switch Activated
    ActorUnload = 35,             // Both Ways                                    High                                  On Actor unloaded
    GeyserTrigger = 36,           // Both Ways                                    High                                  On Geyser Fired
    MapUnlock = 37,               // Both Ways                                    Low                                   On Geyser Fired
    InitialMapEntries = 38,       // Server -> Client                             Low (depends on join)                 Map on Load
    GardenPlant = 39,             // Both Ways                                    Middle (manual)                       On Food Planted
    AccessDoor = 40,              // Both Ways                                    Low                                   On Map Extension bought
    InitialAccessDoors = 41,      // Both Ways                                    Low (depends on join)                 Access Doors on Load
    ResourceAttach = 42,          // Both Ways                                    Very High                             On Resource Attach
    WeatherUpdate = 43,           // Server -> All Clients                        High                                  On Weather Update
    InitialWeather = 44,          // Server -> Client                             Low (depends on join)                 Weather on Load
    LightningStrike = 45,         // Both Ways                                    Low                                   On Lightning Strike
    RefineryUpdate = 46,          // Both Ways                                    Middle (can be manual)                On Refinery Change
    InitialRefinery = 47,         // Server -> Client                             Low (depends on join)                 Refinery items on join
    LandPlotUpgrade = 48,         // Both Ways                                    Low (manual)                          Land plot upgrade
    PlayerGadgetUpdate = 49,      // Both Ways                                    Very High (depends on player mode)    Gadget Mode toggle
    ReservedAck = 254,            // Both Ways                                    Very High                             For ACK packets
    ReservedCompression = 255     // Both Ways                                    Very High                             For packet compression
}