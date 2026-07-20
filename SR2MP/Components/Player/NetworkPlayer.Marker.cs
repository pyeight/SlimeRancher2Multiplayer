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
    
    private GameObject? compassRender;

    internal void SetCompassRenderInstance(GameObject instance) => compassRender = instance;

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

        compassRender = null;

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

        UpdateDistanceLabel();

        if (!PlayerMarkerTransforms.TryGetValue(ID, out var marker)) return;
        if (!marker.mainMarker || !marker.markerArrow) return;

        var pos = transform.position;
        marker.mainMarker!.localPosition = new Vector3(pos.x, pos.z, 0);
        marker.markerArrow!.eulerAngles = new Vector3(0, 0, -transform.eulerAngles.y);
    }

    private TextMeshProUGUI? GetCompassLabel(int childIndex)
    {
        var target = compassRender ? compassRender : radarComponent?._compassRadarPrefab;
        return target ? target!.transform.GetChild(childIndex).GetComponent<TextMeshProUGUI>() : null;
    }

    private void UpdateDistanceLabel()
    {
        if (!SceneContext.Instance.player || model == null) return;

        var distanceLabel = GetCompassLabel(1);
        if (distanceLabel == null) return;

        var distance = Vector3.Distance(transform.position, SceneContext.Instance.player.transform.position);
        var text = $"({Mathf.RoundToInt(distance)}m)";
        
        // var colorHex = ColorUtility.ToHtmlStringRGB(RemotePlayerManager.GetPlayerColor(model));
        var colorHex = ExtractUsernameColorHex(model.Username);

        distanceLabel.SetText(colorHex != null ? $"<color=#{colorHex}>{text}</color>" : text);
    }
    
    private static string? ExtractUsernameColorHex(string username)
    {
        const string tagStart = "<color=#";
        var startIndex = username.IndexOf(tagStart, StringComparison.Ordinal);
        if (startIndex < 0) return null;

        startIndex += tagStart.Length;
        var endIndex = username.IndexOf('>', startIndex);
        return endIndex < 0 ? null : username[startIndex..endIndex];
    }
}