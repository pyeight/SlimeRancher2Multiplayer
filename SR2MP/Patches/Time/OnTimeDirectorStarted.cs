using HarmonyLib;
using SR2E.Utils;
using SR2MP.Components.Time;

namespace SR2MP.Patches.Time;

[HarmonyPatch(typeof(SceneContext), nameof(SceneContext.Start))]
public static class OnTimeDirectorStarted
{
    private static bool injectedToServer;

    public static void Postfix(SceneContext __instance)
    {
        if (Main.Server == null)
            return;

        // This is temporary until we have a proper GUI (we should not host in the menu)
        if (Main.Server.IsRunning())
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
