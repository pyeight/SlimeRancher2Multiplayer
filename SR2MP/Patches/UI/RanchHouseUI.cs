using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.UI;

namespace SR2MP.Patches.UI;

[HarmonyPatch]
internal static class RanchHouseUIPatch
{
    [HarmonyPatch(typeof(RanchHouseUI), nameof(RanchHouseUI.Awake))]
    [HarmonyPostfix]
    private static void OnOpen() => IsInRanchHouse = true;

    [HarmonyPatch(typeof(RanchHouseUI), nameof(RanchHouseUI.OnDestroy))]
    [HarmonyPostfix]
    private static void OnClose() => IsInRanchHouse = false;
}