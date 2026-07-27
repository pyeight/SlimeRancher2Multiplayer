using System.Collections;
using HarmonyLib;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Weather;

[HarmonyPatch(typeof(SceneContext), nameof(SceneContext.Start))]
internal static class OnSceneContextStart
{
    private static bool subscribed;

    public static void Postfix()
    {
        if (subscribed) return;
        subscribed = true;

        Main.Server.OnServerStarted += () => StartCoroutine(TornadoLoop());
        Main.Client.OnConnected += _ => StartCoroutine(TornadoLoop());
    }

    private static IEnumerator TornadoLoop()
    {
        while (Main.Server.IsRunning || Main.Client.IsConnected)
        {
            yield return new WaitForSeconds(0.5f);

            try { NetworkTornadoManager.HostTick(); }
            catch (Exception ex) { SrLogger.LogWarning($"HostTick: {ex.Message}"); }

            try { NetworkTornadoManager.ClientTick(); }
            catch (Exception ex) { SrLogger.LogWarning($"ClientTick: {ex.Message}"); }
        }
    }
}
