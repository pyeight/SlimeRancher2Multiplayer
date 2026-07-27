using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.UI.Map;
using SR2MP.Components.Player;

namespace SR2MP.Patches.Map;

[HarmonyPatch(typeof(MapUI), nameof(MapUI.OnDestroy))]
internal static class OnMapUIDestroy
{
    public static void Prefix(MapUI __instance)
    {
        if (ActiveMapUI == __instance)
            ActiveMapUI = null;
        
        foreach (var playerObj in PlayerObjects.Values)
        {
            if (!playerObj)
                continue;

            var networkPlayer = playerObj.GetComponent<NetworkPlayer>();
            if (networkPlayer != null)
                networkPlayer.DestroyMapMarker();
        }

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