using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Drone;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Drone;

[HarmonyPatch(typeof(DiscoverableResourceDataStore), nameof(DiscoverableResourceDataStore.UpdateResourceData))]
internal static class OnDroneResourceUpdate
{
    public static void Postfix(DiscoverableResourceDataStore __instance, int index, ref DiscoverableResourceData updatedData)
    {
        if (HandlingPacket) return;

        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;

        try
        {
            Main.SendToAllOrServer(new DroneResourcePacket
            {
                Scene = NetworkSceneManager.GetPersistentID(__instance.SceneGroup),
                Index = index,
                Entry = NetworkDroneManager.CreateEntry(updatedData)
            });
        }
        catch (Exception ex)
        {
            SrLogger.LogDebug($"Failed to sync drone resource update: {ex.Message}");
        }
    }
}
