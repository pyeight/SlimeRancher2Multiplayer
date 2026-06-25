using HarmonyLib;
using SR2MP.Components.LandPlots;

namespace SR2MP.Patches.LandPlots;

[HarmonyPatch(typeof(SpawnResource), nameof(SpawnResource.Awake))]
internal static class SpawnResourceAwakePatch
{
    public static void Postfix(SpawnResource __instance)
    {
        if (!Main.Server.IsRunning || !Main.Client.IsConnected) return;
        
        if (__instance.gameObject.GetComponent<NetworkGarden>() == null)
            __instance.gameObject.AddComponent<NetworkGarden>();
    }
}