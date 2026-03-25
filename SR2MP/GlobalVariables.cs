using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.UI;
// using Il2CppMonomiPark.SlimeRancher.Weather;
// using Il2CppMonomiPark.SlimeRancher.World;
using SR2MP.Shared.Managers;
using PriceDictionary = Il2CppSystem.Collections.Generic.Dictionary<Il2Cpp.IdentifiableType, Il2CppMonomiPark.SlimeRancher.Economy.PlortEconomyDirector.CurrValueEntry>;

namespace SR2MP;

public static class GlobalVariables
{
    internal static bool devMode = true;
    public static readonly string[] CheatCommands = {
        "actortype", "clearinv", "delwarp", "emotions", "fastforward", "flatlook", "fling", "floaty", "freeze",
        "fxplayer", "gadget", "give", "gordo", "gravity", "infenergy", "infhealth", "kill", "killall", "newbucks",
        "noclip", "pedia", "player", "position", "ranch", "refillinv", "replace", "rotation", "scale",
        "setwarp", "spawn", "speed", "strike", "timescale", "upgrade", "warp", "warplist", "weather"
    };

    public static bool cheatsEnabled = false;

    internal static GameObject playerPrefab;

    public static readonly Dictionary<string, GameObject> PlayerObjects = new();

    public static readonly RemotePlayerManager PlayerManager = new();

    public static readonly RemoteFXManager FXManager = new();

    public static readonly NetworkActorManager ActorManager = new();

    // public static Dictionary<string, GameObject> landPlotObjects = new();

    // public static Dictionary<ZoneDefinition, Dictionary<string, WeatherPatternDefinition>> weatherPatternsByZone;

    // public static Dictionary<string, WeatherPatternDefinition> weatherPatternsFromStateNames;

    // To prevent stuff from being stuck in
    // an infinite sending loop
    public static bool HandlingPacket { get; internal set; }

    public static string LocalID =>
        Main.Server.IsRunning()
            ? Main.Server.PlayerId
            : Main.Client.IsConnected
                ? Main.Client.PlayerId
                : string.Empty;

    public static (float Current, float Previous)[]? MarketPricesArray => SceneContext.Instance
        ? Array.ConvertAll<PriceDictionary.Entry, (float Current, float Previous)>(
            SceneContext.Instance.PlortEconomyDirector._currValueMap._entries,
            entry => (entry.value?.CurrValue ?? 0f, entry.value?.PrevValue ?? 0f))
        : null;

    public static MarketUI? MarketUIInstance { get; internal set; }

    public const string MapEventKey = "fogRevealed";

    public const byte HeaderSize = 12;

    // Constants for ammo types
    public const string SiloAmmo = "58d5bd4fc903e1c49aba61495aa74014";
    public const string PlortCollectorAmmo = "83f638af7ebb11944b6b55c915889459";
    // This is the duplicate PlotAmmoSetDefinition, the Coop Collector
    public const string CoopAmmo = "e65dad0e2c627f8498d5a2b3b65f6215";
    public const string FeederAmmo = "7e1edc80785d7894a928f24f5aebbccd";

    // Shortcut Properties
    public static GameModel GameState => SceneContext.Instance.GameModel;

    public static string[] Mods { get; internal set; }
}