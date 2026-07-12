using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.UI.RanchHouse;

namespace SR2MP.Patches.UI;

[HarmonyPatch]
internal static class RanchHouseUIPatch
{
    [HarmonyPatch(typeof(RanchHouseMenuRoot), nameof(RanchHouseMenuRoot.Awake))]
    [HarmonyPostfix]
    private static void OnOpen() => IsInRanchHouse = true;

    [HarmonyPatch(typeof(RanchHouseMenuItemModel), nameof(RanchHouseMenuItemModel.CloseMenu))]
    [HarmonyPostfix]
    private static void OnClose() => IsInRanchHouse = false;
}