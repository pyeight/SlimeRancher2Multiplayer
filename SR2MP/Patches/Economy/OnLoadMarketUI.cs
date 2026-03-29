using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.UI;

namespace SR2MP.Patches.Economy;

[HarmonyPatch(typeof(MarketUI), nameof(MarketUI.Start))]
internal static class OnLoadMarketUI
{
    public static void Postfix(MarketUI __instance)
    {
        MarketUIInstance = __instance;
    }
}