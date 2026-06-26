using Il2CppMonomiPark.SlimeRancher.UI;
using Il2CppMonomiPark.SlimeRancher.UI.Map;
using Il2CppTMPro;
using SR2MP.Shared.Managers;
using UnityEngine.UI;

namespace SR2MP.Components.Player;

internal partial class NetworkPlayer
{
    private RadarTrackedPointOfInterest? radarComponent;
    private bool markerVisible;

    private void SetupMarker()
    {
        if (IsLocal) return;

        radarComponent = gameObject.AddComponent<RadarTrackedPointOfInterest>();
        radarComponent.enabled = false;
        radarComponent._worldRadarPrefab = null;
        radarComponent._compassRadarPrefab = Instantiate(PlayerCompassPrefab);
        radarComponent._isOptional = false;
        radarComponent._overflowMode = RadarCompassOverflowMode.CLAMP;
        radarComponent._ranchBehaviour = RadarEntryRanchHandling.SHOW_IN_RANCH_AS_WELL;

        SrLogger.LogDebug($"Remote player marker added: {model!.PlayerId}");
    }

    private void RefreshMarker()
    {
        if (radarComponent)
        {
            Destroy(radarComponent);
            radarComponent = null;
        }

        SetupMarker();

        if (model != null)
            SetUsername(model.Username);
    }

    public void CreateMapMarker(MapUI mapUI)
    {
        if (IsLocal) return;

        if (PlayerMarkerTransforms.TryGetValue(ID, out var existingMarker))
        {
            if (existingMarker.mainMarker != null)
                Destroy(existingMarker.mainMarker.gameObject);
        }
        else
        {
            PlayerMarkerTransforms[ID] = new();
        }

        var marker = Instantiate(
            mapUI._markerPrefabMapping._playerMarkerPrefab, 
            mapUI._mapContainer.transform.parent.FindChild("Markers"), 
            true);
        
        marker.transform.position = new Vector3(transform.position.x, transform.position.z, 0);
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
        textComponent.SetText(model!.Username);
        textComponent.alpha = 0.6f;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.font = usernameFont;
        textComponent.overflowMode = TextOverflowModes.Overflow;
        textComponent.enableWordWrapping = false;
        
        var facingFrame = marker.transform.FindChild("FacingFrame");
        facingFrame.FindChild("FacingArrow").GetComponent<Image>().m_Color = RemotePlayerManager.GetPlayerColor(model);
        
        var markerTransformGroup = PlayerMarkerTransforms[ID];
        markerTransformGroup.mainMarker = marker.transform;
        markerTransformGroup.markerArrow = facingFrame.transform;
    }

    private void UpdateMarker()
    {
        if (IsLocal) return;

        var sameSceneGroup = IsInLocalSceneGroup();

        if (!radarComponent)
        {
            SetupMarker();
            if (model != null)
                SetUsername(model.Username);
        }

        if (radarComponent)
        {
            if (sameSceneGroup && !markerVisible)
            {
                RefreshMarker();
                markerVisible = true;
                return;
            }

            radarComponent!.enabled = sameSceneGroup;
            markerVisible = sameSceneGroup;
        }

        if (!sameSceneGroup) return;

        if (!PlayerMarkerTransforms.TryGetValue(ID, out var marker)) return;
        if (!marker.mainMarker || !marker.markerArrow) return;

        var pos = transform.position;
        marker.mainMarker!.localPosition = new Vector3(pos.x, pos.z, 0);
        marker.markerArrow!.eulerAngles = new Vector3(0, 0, -transform.eulerAngles.y);
    }

    private bool IsInLocalSceneGroup()
    {
        var localSceneGroup = NetworkSceneManager.GetPersistentID(
            SystemContext.Instance.SceneLoader._currentSceneGroup);
        return (model?.SceneGroup ?? -1) == localSceneGroup;
    }
}