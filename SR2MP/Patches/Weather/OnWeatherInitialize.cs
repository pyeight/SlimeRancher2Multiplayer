using HarmonyLib;
using Starlight.Utils;
using SR2MP.Components.World;

namespace SR2MP.Patches.Weather;

[HarmonyPatch(typeof(SceneContext), nameof(SceneContext.Start))]
internal static class OnWeatherInitialize
{
    private static bool injectedToServer;

    public static void Postfix(SceneContext __instance)
    {
        // This is temporary until we have a proper GUI (we should not host in the menu)
        if (Main.Server.IsRunning)
        {
            __instance.gameObject.AddComponent<NetworkWeather>();
        }
        else if (!injectedToServer)
        {
            Main.Server.OnServerStarted += () => SceneContext.Instance.AddComponent<NetworkWeather>();
            injectedToServer = true;
        }
    }
}