using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Drone;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Drone;

[HarmonyPatch(typeof(RanchDrone), nameof(RanchDrone.Awake))]
internal static class OnRanchDroneAwake
{
    public static void Postfix(RanchDrone __instance)
    {
        NetworkDroneManager.CheckSubscribed();

        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;

        NetworkDroneManager.GetNetworkComponent(__instance.gameObject);
    }
}

[HarmonyPatch(typeof(ExplorerDrone), nameof(ExplorerDrone.Awake))]
internal static class OnExplorerDroneAwake
{
    public static void Postfix(ExplorerDrone __instance)
    {
        NetworkDroneManager.CheckSubscribed();

        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;

        NetworkDroneManager.GetNetworkComponent(__instance.gameObject);
    }
}
