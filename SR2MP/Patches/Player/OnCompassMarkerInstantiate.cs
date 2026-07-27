using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.UI;
using SR2MP.Components.Player;

namespace SR2MP.Patches.Player;

[HarmonyPatch(typeof(RadarTrackedPointOfInterest),
                                       // Not sure if theres a better way but if there is, change this 
    nameof(RadarTrackedPointOfInterest.MonomiPark_SlimeRancher_UI_IRadarEntry_InstantiateCompassMarkerPrefab))]
internal static class OnCompassMarkerInstantiate
{
    public static void Postfix(RadarTrackedPointOfInterest __instance, GameObject __result)
    {
        var networkPlayer = __instance.GetComponent<NetworkPlayer>();
        networkPlayer?.SetCompassRenderInstance(__result);
    }
}
