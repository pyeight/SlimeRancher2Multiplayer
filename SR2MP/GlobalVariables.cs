using Il2CppMonomiPark.SlimeRancher.UI;
using SR2MP.Shared.Managers;
using PriceDictionary = Il2CppSystem.Collections.Generic.Dictionary<Il2Cpp.IdentifiableType, Il2CppMonomiPark.SlimeRancher.Economy.PlortEconomyDirector.CurrValueEntry>;

namespace SR2MP;

public static class GlobalVariables
{
    public static readonly string[] CheatCommands = {
        "actortype", "clearinv", "delwarp", "emotions", "fastforward", "flatlook", "fling", "floaty", "freeze",
        "fxplayer", "gadget", "give", "gordo", "gravity", "infenergy", "infhealth", "kill", "killall", "newbucks",
        "noclip", "party", "pedia", "player", "position", "ranch", "refillinv", "replace", "rotation", "scale",
        "setwarp", "spawn", "speed", "strike", "timescale", "upgrade", "warp", "warplist", "weather",
    };

    public static bool CheatsEnabled = false;

    internal static GameObject playerPrefab;

    public static Dictionary<string, GameObject> playerObjects = new();

    public static RemotePlayerManager playerManager = new RemotePlayerManager();

    public static RemoteFXManager fxManager = new RemoteFXManager();

    public static NetworkActorManager actorManager = new NetworkActorManager();

    public static Dictionary<string, GameObject> landPlotObjects = new();

    // To prevent stuff from being stuck in
    // an infinite sending loop
    public static bool handlingPacket = false;

    // I love this indenting
    public static string LocalID =>
        Main.Server.IsRunning()
            ? "HOST"
            : Main.Client.IsConnected
                ? Main.Client.OwnPlayerId
                : string.Empty;

    public static (float Current, float Previous)[]? MarketPricesArray => SceneContext.Instance
        ? Array.ConvertAll<PriceDictionary.Entry, (float Current, float Previous)>(
            SceneContext.Instance.PlortEconomyDirector._currValueMap._entries,
            entry => (entry.value?.CurrValue ?? 0f, entry.value?.PrevValue ?? 0f))
        : null;

    public static MarketUI? marketUIInstance;

    public const string MapEventKey = "fogRevealed";
}