using HarmonyLib;
using Starlight.Utils;
using SR2MP.Components.Time;

namespace SR2MP.Patches.Time;

[HarmonyPatch(typeof(SceneContext), nameof(SceneContext.Start))]
internal static class OnTimeDirectorStarted
{
    private static bool injectedToServer;

    public static void Postfix(SceneContext __instance)
    {
        // This is temporary until we have a proper GUI (we should not host in the menu)
        if (Main.Server.IsRunning)
        {
            __instance.gameObject.AddComponent<NetworkTime>();
        }
        else if (!injectedToServer)
        {
            Main.Server.OnServerStarted += () => SceneContext.Instance.AddComponent<NetworkTime>();
            injectedToServer = true;
        }
    }
}