using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Map;
using Il2CppMonomiPark.SlimeRancher.UI.Map;
using Il2CppTMPro;
using SR2MP.Client.Models;
using SR2MP.Components.Player;
using SR2MP.Shared.Managers;
using UnityEngine.UI;

namespace NewSR2MP.Patches;

[HarmonyPatch(typeof(MapUI), nameof(MapUI.Start))]
internal class OnMapUIAppear
{
    public static void Postfix(MapUI __instance)
    {
        foreach (var player in PlayerManager.GetAllPlayers())
        {
            if (player.PlayerId == LocalID)
                continue;
            
            var marker = Object.Instantiate(
                __instance._markerPrefabMapping._playerMarkerPrefab, 
                __instance._mapContainer.transform.parent.FindChild("Markers"), 
                true);
            
            marker.transform.position = new Vector3(player.Position.x, player.Position.z, 0);
            marker.transform.localScale = Vector3.one;

            marker.GetComponent<MapFader>()._targetOpacity = 100;
            
            var textObject = new GameObject("PlayerName")
            {
                transform =
                {
                    parent = marker.transform,
                    localPosition = new Vector3(0, 42, 0),
                    localScale = Vector3.one * 0.6f,
                }
            };
            
            var textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textComponent.SetText(player.Username);
            textComponent.alpha = 0.6f;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.font = PlayerObjects[player.PlayerId].GetComponent<NetworkPlayer>().usernameFont;
            textComponent.overflowMode = TextOverflowModes.Overflow;
            textComponent.enableWordWrapping = false;
            
            var facingFrame = marker.transform.FindChild("FacingFrame");
            facingFrame.FindChild("FacingArrow").GetComponent<Image>().m_Color = RemotePlayerManager.GetPlayerColor(player);
            
            var markerTransformGroup = PlayerMarkerTransforms[player.PlayerId];
            markerTransformGroup.mainMarker = marker.transform;
            markerTransformGroup.markerArrow = facingFrame.transform;
        }
    }
}