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
    BroadcastChatMessage = 10,    // Server -> All Clients                        Unused                                Chat message on other Clients
    Heartbeat = 11,               // Client -> Server                             Low (Currently unused)                Check if Clients are alive
    HeartbeatAck = 12,            // Server -> Client                             Low (depends on heartbeat)            Automatically time the Clients out if the Server crashes
    WorldTime = 13,               // Server -> All Clients                        Very High (depends on join too)       Updates Time
    FastForward = 14,             // Client -> Server                             Middle (can be manual)                On Sleep & Death
    BroadcastFastForward = 15,    // Server -> All Clients                        Middle (depends on forward/death)     On Sleep & Death on other clients
    PlayerFX = 16,                // Both Ways                                    Middle (manual)                       On Player FX
    MovementSound = 17,           // Both Ways                                    Middle (manual)                       On Movement SoundPlay
    CurrencyAdjust = 18,          // Both Ways                                    High (can be manual)                  On Plort sell
    ActorDestroy = 19,            // Both Ways                                    High (can be manual)                  On Actor Destroy
    ActorSpawn = 20,              // Both Ways                                    High (can be manual)                  On Actor Spawn
    ActorUpdate = 21,             // Both Ways                                    Very High                             On Actor Update
    ActorTransfer = 22,           // Both Ways                                    High (can be manual)                  On Actor Transfer
    InitialActors = 23,           // Server -> Client                             Low                                   Actors on Load
    LandPlotUpdate = 24,          // Both Ways                                    Low (manual)                          Land plot updates (upgrade or set)
    InitialPlots = 25,            // Server -> Client                             Low                                   Plots on Load
    WorldFX = 26,                 // Both Ways                                    High                                  On World FX
    InitialPlayerUpgrades = 27,   // Server -> Client                             Low                                   Player Upgrades on Load
    PlayerUpgrade = 28,           // Both Ways                                    Low (manual)                          On Upgrade
    InitialPediaEntries = 29,     // Server -> Client                             Low                                   Pedia Entries on Load
    PediaUnlock = 30,             // Both Ways                                    Middle (manual)                       On Pedia entry
    MarketPriceChange = 31,       // Both Ways                                    Middle                                On Plort Market Price Change
    GordoFeed = 32,               // Both Ways                                    Middle (manual)                       On Gordo Fed
    GordoBurst = 33,              // Both Ways                                    Low (depends on feed)                 On Gordo Burst
    InitialGordos = 34,           // Server -> Client                             Low (depends on join)                 Gordos on Load
    InitialSwitches = 35,         // Server -> Client                             Low (depends on join)                 Switches on Load
    SwitchActivate = 36,          // Both Ways                                    Low                                   On Switch Activated
    ActorUnload = 37,             // Both Ways                                    High                                  On Actor unloaded
    GeyserTrigger = 38,           // Both Ways                                    High                                  On Geyser Fired
    MapUnlock = 39,               // Both Ways                                    Low                                   On Geyser Fired
    InitialMapEntries = 40,       // Server -> Client                             Low (depends on join)                 Map on Load
    GardenPlant = 41,             // Both Ways                                    Middle (manual)                       On Food Planted
    AccessDoor = 42,              // Both Ways                                    Low                                   On Map Extension bought
    InitialAccessDoors = 43,      // Both Ways                                    Low (depends on join)                 Access Doors on Load
    ResourceAttach = 44,          // Both Ways                                    Very High                             On Resource Attach
    WeatherUpdate = 45,           // Server -> All Clients                        High                                  On Weather Update
    InitialWeather = 46,          // Server -> Client                             Low (depends on join)                 Weather on Load
    LightningStrike = 47,         // Both Ways                                    Low                                   On Lightning Strike
    RefineryUpdate = 48,          // Both Ways                                    Middle (can be manual)                On Refinery Change
    InitialRefinery = 49,         // Server -> Client                             Low (depends on join)                 Refinery items on join
    ReservedAck = 254,            // Both Ways                                    Very High                             For ACK packets
    ReservedCompression = 255 // Not Used                                     Unused                                For packet compression
}