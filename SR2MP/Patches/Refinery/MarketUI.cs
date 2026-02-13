using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.UI;

namespace SR2MP.Patches.Refinery;

[HarmonyPatch(typeof(MarketUI), nameof(MarketUI.Awake))]
public static class MarketUIAwake
{
    public static void Prefix(MarketUI __instance)
    {
        marketUI = __instance;
    }
}