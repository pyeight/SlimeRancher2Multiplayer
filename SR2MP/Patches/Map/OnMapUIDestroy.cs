using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.UI.Map;

namespace SR2MP.Patches.Map;

[HarmonyPatch(typeof(MapUI), nameof(MapUI.OnDestroy))]
internal static class OnMapUIDestroy
{
    public static void Prefix(MapUI __instance)
    {
        if (OnMapUIStart.activeMapUI == __instance)
            OnMapUIStart.activeMapUI = null;

        foreach (var pair in PlayerMarkerTransforms)
        {
            var marker = pair.Value;
            if (marker.mainMarker != null)
                Object.Destroy(marker.mainMarker.gameObject);
            
            marker.mainMarker = null;
            marker.markerArrow = null;
        }
    }
}