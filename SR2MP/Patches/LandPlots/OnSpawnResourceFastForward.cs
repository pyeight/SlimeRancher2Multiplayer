// Memory Access Violation, keep it here
/*
 using HarmonyLib;
using SR2MP.Components.LandPlots;

namespace SR2MP.Patches.LandPlots;

[HarmonyPatch(typeof(SpawnResource), nameof(SpawnResource.FastForward))]
internal static class OnSpawnResourceFastForward
{
    public static bool Prefix(SpawnResource __instance)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return true;

        __instance.TryGetComponent<NetworkGarden>(out var networkGarden);
        if (networkGarden == null) return true;

        return networkGarden.LocallyOwned;
    }
}
*/