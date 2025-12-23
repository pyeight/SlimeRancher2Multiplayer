using SR2MP.Shared.Managers;
using UnityEngine;

namespace SR2MP;

public static class GlobalVariables
{
    internal static GameObject playerPrefab;

    public static Dictionary<string, GameObject> playerObjects = new();

    public static RemotePlayerManager playerManager = new RemotePlayerManager();
    
    public static RemoteFXManager fxManager = new RemoteFXManager();

    // To prevent stuff from being stuck in 
    // an infinite sending loop qwq
    public static bool handlingPacket = false;

    // I love this indenting
    public static string LocalID =>
        Main.Server.IsRunning() 
            ? "HOST"
            : Main.Client.IsConnected
                ? Main.Client.OwnPlayerId
                : "";
}