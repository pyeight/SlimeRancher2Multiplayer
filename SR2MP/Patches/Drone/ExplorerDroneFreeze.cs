using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Drone;
using SR2MP.Components.Drone;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Drone;

[HarmonyPatch(typeof(ExplorerDroneMovementManager), nameof(ExplorerDroneMovementManager.FixedUpdate))]
internal static class ExplorerDroneMovementFreeze
{
    public static bool Prefix(ExplorerDroneMovementManager __instance)
    {
        var networkDrone = __instance.GetComponentInParent<NetworkDrone>();
        
        if (networkDrone == null && (Main.Server.IsRunning || Main.Client.IsConnected))
        {
            var explorerDrone = __instance.GetComponentInParent<ExplorerDrone>();
            if (explorerDrone)
                networkDrone = NetworkDroneManager.GetNetworkComponent(explorerDrone!.gameObject);
        }

        return networkDrone == null || networkDrone.LocallyOwned;
    }
}

[HarmonyPatch(typeof(ExplorerDroneProgram), nameof(ExplorerDroneProgram.Update))]
internal static class ExplorerDroneProgramFreeze
{
    public static bool Prefix(ExplorerDroneProgram __instance)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
            return true;

        var stationModel = __instance._stationModel;
        if (stationModel == null)
            return true;

        if (NetworkDrone.Drones.TryGetValue(stationModel.actorId.Value, out var drone) && drone &&
            !drone.IsHibernated && !drone.LocallyOwned)
            return false;

        return true;
    }
}
