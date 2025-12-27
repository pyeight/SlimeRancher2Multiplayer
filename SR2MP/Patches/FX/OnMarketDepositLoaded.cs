using HarmonyLib;
using SR2E.Utils;
using SR2MP.Components.FX;

namespace SR2MP.Patches.FX;

[HarmonyPatch(typeof(ScorePlort), nameof(ScorePlort.Start))]
public static class OnMarketDepositLoaded
{
    public static void Postfix(ScorePlort __instance)
    {
        fxManager.sellFX = __instance.ExplosionFX;
    }
}