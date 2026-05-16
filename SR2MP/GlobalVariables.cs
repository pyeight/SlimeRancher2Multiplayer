using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.UI;
using SR2MP.Components.Player;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;
using PriceDictionary = Il2CppSystem.Collections.Generic.Dictionary<Il2Cpp.IdentifiableType, Il2CppMonomiPark.SlimeRancher.Economy.PlortEconomyDirector.CurrValueEntry>;

namespace SR2MP;

/// <summary>
/// Provides global state variables, constants, and core manager instances for the multiplayer environment.
/// </summary>
public static class GlobalVariables
{
    /// <summary>
    /// Gets or sets a value indicating whether development mode is currently active.
    /// </summary>
    public static bool DevMode { get; internal set; } = true;

    internal static readonly string[] CheatCommands = {
        "actortype", "clearinv", "delwarp", "emotions", "fastforward", "flatlook", "fling", "floaty", "freeze",
        "fxplayer", "gadget", "give", "gordo", "gravity", "infenergy", "infhealth", "kill", "killall", "newbucks",
        "noclip", "pedia", "player", "position", "ranch", "refillinv", "replace", "rotation", "scale",
        "setwarp", "spawn", "speed", "strike", "timescale", "upgrade", "warp", "warplist", "weather"
    };

    /// <summary>
    /// Gets or sets a value indicating whether cheat commands are enabled in the current session.
    /// </summary>
    public static bool CheatsEnabled { get; internal set; }

    /// <summary>
    /// Gets or sets the Unity GameObject prefab used for the <see cref="NetworkPlayer"/> compass marker.
    /// </summary>
    public static GameObject PlayerCompassPrefab  { get; internal set; }
    
    /// <summary>
    /// Gets or sets the Unity GameObject prefab used for the <see cref="NetworkPlayer"/> map marker.
    /// </summary>
    public static GameObject PlayerMapPrefab  { get; internal set; }
    
    /// <summary>
    /// Gets or sets the base Unity GameObject prefab used for instantiating remote players.
    /// </summary>
    public static GameObject PlayerPrefab { get; internal set; }
    internal static readonly Dictionary<string, GameObject> PlayerObjects = new();
    
    /// <summary>
    /// The core manager responsible for tracking and handling remote players.
    /// </summary>
    public static readonly RemotePlayerManager PlayerManager = new();

    internal static readonly RemoteFXManager FXManager = new();
    internal static readonly NetworkActorManager ActorManager = new();

    // To prevent stuff from being stuck in an infinite sending loop
    /// <summary>
    /// Gets or sets a value indicating whether a network packet is currently being processed.
    /// </summary>
    public static bool HandlingPacket { get; internal set; }

    /// <summary>
    /// Gets the local identifier for the current instance, dynamically checking whether it is acting as the server or a client.
    /// </summary>
    public static string LocalID =>
        Main.Server.IsRunning
            ? Main.Server.PlayerId
            : (Main.Client.IsConnected
                ? Main.Client.PlayerId
                : string.Empty);

    internal static (float Current, float Previous)[]? MarketPricesArray => SceneContext.Instance
        ? Array.ConvertAll<PriceDictionary.Entry, (float Current, float Previous)>(
            SceneContext.Instance.PlortEconomyDirector._currValueMap._entries,
            entry => (entry.value?.CurrValue ?? 0f, entry.value?.PrevValue ?? 0f))
        : null;

    /// <summary>
    /// Gets or sets the currently active Market UI instance.
    /// </summary>
    public static MarketUI? MarketUIInstance { get; internal set; }

    /// <summary>
    /// The dictionary key representing the event of fog being revealed on the map.
    /// </summary>
    public const string MapEventKey = "fogRevealed";

    internal const byte HeaderSize = 13;

    internal const int ActorIdOffset = 1000000;

    // Constants for ammo types

    /// <summary>
    /// The definition ID for silo storage ammo.
    /// </summary>
    public const string SiloAmmo = "58d5bd4fc903e1c49aba61495aa74014";

    /// <summary>
    /// The definition ID for the standard plort collector ammo.
    /// </summary>
    public const string PlortCollectorAmmo = "83f638af7ebb11944b6b55c915889459";

    // This is the duplicate PlotAmmoSetDefinition, the Coop Collector
    /// <summary>
    /// The definition ID for the coop collector ammo.
    /// </summary>
    public const string CoopAmmo = "e65dad0e2c627f8498d5a2b3b65f6215";

    /// <summary>
    /// The definition ID for auto-feeder ammo.
    /// </summary>
    public const string FeederAmmo = "7e1edc80785d7894a928f24f5aebbccd";

    // Shortcut Properties

    /// <summary>
    /// Gets the current game state model from the active scene context.
    /// </summary>
    public static GameModel GameState => SceneContext.Instance.GameModel;
    
    internal static readonly Dictionary<string, MarkerTransform> PlayerMarkerTransforms = new();
}