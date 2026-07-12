using HarmonyLib;

namespace SR2MP.Patches.FX;

[HarmonyPatch(typeof(SplashOnTrigger), nameof(SplashOnTrigger.Awake))]
internal static class SplashOnTriggerAwake
{
    public static void Postfix(SplashOnTrigger __instance)
    {
        if (__instance.playerSplashFX != null)
            try { FXManager.PlayerFXMap[PlayerFXType.WaterSplash] = __instance.playerSplashFX; } catch { /* ignored */ }
    }
}