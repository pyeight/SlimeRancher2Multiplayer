using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Drone;
using SR2MP.Components.Drone;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Drone;

[HarmonyPatch(typeof(RanchDrone), nameof(RanchDrone.RegistryFixedUpdate))]
internal static class RanchDroneFreeze
{
    private static readonly Dictionary<IntPtr, float> LastRecovery = new();

    public static bool Prefix(RanchDrone __instance)
    {
        var networkDrone = __instance.GetComponent<NetworkDrone>();
        
        if (networkDrone == null && (Main.Server.IsRunning || Main.Client.IsConnected))
            networkDrone = NetworkDroneManager.GetNetworkComponent(__instance.gameObject);

        return networkDrone == null || networkDrone.LocallyOwned;
    }

    public static Exception? Finalizer(Exception? __exception, RanchDrone __instance)
    {
        if (__exception == null)
            return null;
        
        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
            return __exception;

        try
        {
            var now = UnityEngine.Time.unscaledTime;
            var key = __instance.Pointer;

            if (LastRecovery.TryGetValue(key, out var lastTime) && now - lastTime < 3f)
                __instance._droneStation?.AssignTaskNone();
            else
                __instance.ResetToStation();

            LastRecovery[key] = now;
        }
        catch (Exception exception)
        {
            SrLogger.LogWarning($"Ranch drone recovery failed, destroying the drone: {exception.Message}");

            try { __instance.DestroyDrone(); }
            catch { /* ignored */ }
        }

        return null;
    }
}
