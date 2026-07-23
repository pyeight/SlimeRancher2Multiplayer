using HarmonyLib;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.GordoSlime;

[HarmonyPatch(typeof(GordoEat), nameof(GordoEat.Awake))]
internal static class OnGordoSlimeAwake
{
    public static void Postfix(GordoEat __instance)
        => NetworkGordoSlimeManager.ReapplyOnSpawn(__instance);
}
