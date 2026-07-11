using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Drone;
using SR2MP.Components.Drone;
using SR2MP.Packets.Drone;

namespace SR2MP.Patches.Drone;

[HarmonyPatch(typeof(ExplorerDroneMovementManager), nameof(ExplorerDroneMovementManager.StartScan))]
internal static class OnExplorerDroneStartScan
{
    public static void Postfix(ExplorerDroneMovementManager __instance)
        => ExplorerDroneAnimation.Send(__instance, DroneAnimationPacket.DroneAnimation.Scan);
}

[HarmonyPatch(typeof(ExplorerDroneMovementManager), nameof(ExplorerDroneMovementManager.StartGather))]
internal static class OnExplorerDroneStartGather
{
    public static void Postfix(ExplorerDroneMovementManager __instance)
        => ExplorerDroneAnimation.Send(__instance, DroneAnimationPacket.DroneAnimation.Gather);
}

[HarmonyPatch(typeof(ExplorerDroneMovementManager), nameof(ExplorerDroneMovementManager.StartAcquisitionEvent))]
internal static class OnExplorerDroneStartAcquisition
{
    public static void Postfix(ExplorerDroneMovementManager __instance)
        => ExplorerDroneAnimation.Send(__instance, DroneAnimationPacket.DroneAnimation.Acquisition);
}

[HarmonyPatch(typeof(ExplorerDroneMovementManager), nameof(ExplorerDroneMovementManager.StopCurrentAnimationState))]
internal static class OnExplorerDroneStopAnimation
{
    public static void Postfix(ExplorerDroneMovementManager __instance)
        => ExplorerDroneAnimation.Send(__instance, DroneAnimationPacket.DroneAnimation.StopAnimation);
}

internal static class ExplorerDroneAnimation
{
    internal static void Send(ExplorerDroneMovementManager manager, DroneAnimationPacket.DroneAnimation droneAnimation)
    {
        if (HandlingPacket) return;

        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;

        var networkDrone = manager.GetComponentInParent<NetworkDrone>();
        if (networkDrone == null || !networkDrone.LocallyOwned || networkDrone.StationId == 0)
            return;

        Main.SendToAllOrServer(new DroneAnimationPacket
        {
            StationId = networkDrone.StationId,
            Animation = droneAnimation
        });
    }
}