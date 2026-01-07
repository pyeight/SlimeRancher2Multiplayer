using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.UI;

namespace SR2MP.Patches.Economy;

[HarmonyPatch(typeof(MarketUI), nameof(MarketUI.Start))]
public static class OnLoadMarketUI
{
    public static void Postfix(MarketUI __instance)
    {
        marketUIInstance = __instance;
    }
}