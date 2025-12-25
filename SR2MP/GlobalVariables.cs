using SR2MP.Shared.Managers;

namespace SR2MP;

public static class GlobalVariables
{
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
                : "";
}