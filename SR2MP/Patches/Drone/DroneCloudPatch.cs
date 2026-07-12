using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Drone.Cloud;
using SR2MP.Packets.Drone;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Drone;

[HarmonyPatch(typeof(DroneCloudModel), nameof(DroneCloudModel.AddAmount))]
[HarmonyPatch(typeof(DroneCloudModel), nameof(DroneCloudModel.SetAmount))]
internal static class OnDroneCloudChange
{
    public static void Postfix(DroneCloudModel __instance, IdentifiableType type)
        => DroneCloud.SendAmount(__instance, type);
}

internal static class DroneCloud
{
    public static void SendAmount(DroneCloudModel cloud, IdentifiableType? type)
    {
        if (HandlingPacket) return;

        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;

        if (type == null) return;

        Main.SendToAllOrServer(new DroneCloudPacket
        {
            TypeId = NetworkActorManager.GetPersistentID(type),
            Amount = cloud.GetAmount(type)
        });
    }
}
