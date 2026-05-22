using HarmonyLib;

namespace SR2MP.Patches.FX;

[HarmonyPatch(typeof(ScorePlort), nameof(ScorePlort.Start))]
internal static class OnMarketDepositLoaded
{
    public static void Postfix(ScorePlort __instance)
    {
        FXManager.SellFX = __instance.ExplosionFX;
    }
}