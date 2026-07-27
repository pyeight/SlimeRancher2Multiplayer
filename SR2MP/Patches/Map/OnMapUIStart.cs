using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.UI.Map;
using SR2MP.Components.Player;

namespace SR2MP.Patches.Map;

[HarmonyPatch(typeof(MapUI), nameof(MapUI.Start))]
internal static class OnMapUIStart
{
    public static void Prefix(MapUI __instance)
    {
        ActiveMapUI = __instance;
        foreach (var player in PlayerManager.GetAllPlayers())
        {
            if (player.PlayerId == LocalID)
                continue;
            
            if (PlayerObjects.TryGetValue(player.PlayerId, out var playerObj))
            {
                var networkPlayer = playerObj.GetComponent<NetworkPlayer>();
                if (networkPlayer != null)
                    networkPlayer.CreateMapMarker(__instance);
            }
        }
    }
}