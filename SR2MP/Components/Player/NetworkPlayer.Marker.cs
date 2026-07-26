using Il2CppMonomiPark.SlimeRancher.Map;
using Il2CppMonomiPark.SlimeRancher.SceneManagement;
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

    private PlayerMapMarker? mapMarker;
    private PlayerMapMarkerSource? mapMarkerSource;

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

        DestroyMapMarker();

        if (!PlayerMarkerTransforms.ContainsKey(ID))
            PlayerMarkerTransforms[ID] = new();

        try
        {
            var markerPrefab = mapUI._markerPrefabMapping._playerMarkerPrefab;
            if (markerPrefab == null)
            {
                SrLogger.LogWarning($"CreateMapMarker: player marker prefab null for {model?.PlayerId}");
                return;
            }
            
            var container = mapUI._playerMarker != null
                ? mapUI._playerMarker.transform.parent
                : mapUI._mapContainer.transform.parent.FindChild("Markers");

            var markerObj = Instantiate(markerPrefab, container, true);
            markerObj.transform.localScale = Vector3.one;

            mapMarker = markerObj.GetComponent<PlayerMapMarker>();
            if (mapMarker == null)
            {
                SrLogger.LogWarning($"CreateMapMarker: no PlayerMapMarker component on prefab for {model?.PlayerId}");
                Destroy(markerObj);
                return;
            }

            var fader = markerObj.GetComponent<MapFader>();
            if (fader != null)
                fader._targetOpacity = 100;

            mapMarkerSource = new PlayerMapMarkerSource
            {
                _position = transform.position,
                _sceneGroup = ResolveSceneGroup()
            };

            mapMarker.SetSource(mapMarkerSource.Cast<IMapMarkerSource>());

            StyleMapMarker(markerObj);

            PlayerMarkerTransforms[ID].mainMarker = markerObj.transform;

            PositionMapMarker(mapUI);

            SrLogger.LogDebug($"CreateMapMarker: created player marker for {model?.PlayerId}, sceneGroup={model?.SceneGroup}, worldPos={transform.position}");
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"CreateMapMarker failed for {model?.PlayerId}: {ex.Message}");
        }
    }

    private void StyleMapMarker(GameObject obj)
    {
        // var markerColor = RemotePlayerManager.GetPlayerColor(model!);
        var markerColor = GetUsernameColor(model!.Username);

        var facingFrame = obj.transform.FindChild("FacingFrame");
        if (facingFrame != null)
        {
            var arrow = facingFrame.FindChild("FacingArrow");
            if (arrow != null)
                arrow.GetComponent<Image>().m_Color = markerColor;

            PlayerMarkerTransforms[ID].markerArrow = facingFrame;
        }

        var textObject = new GameObject("PlayerName")
        {
            transform =
            {
                parent = obj.transform,
                localPosition = new Vector3(0, 42, 0),
                localScale = Vector3.one * 0.6f,
            }
        };
        
        var textComponent = textObject.AddComponent<TextMeshProUGUI>();
        textComponent.SetText(model!.Username);
        // We have the color tag, this will be overridden
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.font = usernameFont;
        textComponent.overflowMode = TextOverflowModes.Overflow;
        textComponent.enableWordWrapping = false;
    }

    private SceneGroup? ResolveSceneGroup()
    {
        var id = model?.SceneGroup ?? -1;
        if (id < 0) return null;

        try
        {
            return NetworkSceneManager.GetSceneGroup(id);
        }
        catch
        {
            return null;
        }
    }

    private void PositionMapMarker(MapUI? mapUI)
    {
        var ui = mapUI ?? ActiveMapUI;
        if (mapMarker == null || ui == null || ui._map == null || mapMarker._rectTransform == null)
            return;

        try
        {
            var sceneGroup = mapMarkerSource?._sceneGroup ?? ResolveSceneGroup();
            var visible = IsOnCurrentMap(ui, sceneGroup);

            if (mapMarker.gameObject.activeSelf != visible)
                mapMarker.gameObject.SetActive(visible);

            if (!visible)
                return;

            var mapPos = ui._map.GetMapPosition(transform.position);
            mapMarker._rectTransform.anchoredPosition = new Vector2(mapPos.x, mapPos.y);
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"PositionMapMarker failed for {model?.PlayerId}: {ex.Message}");
        }
    }
    
    private static bool IsOnCurrentMap(MapUI ui, SceneGroup? group)
    {
        if (group == null)
            return false;

        var sceneGroups = ui.CurrentMapDefinition?.RelatedScenes?._gameplaySceneGroups;
        if (sceneGroups == null)
            return false;

        foreach (var sceneGroup in sceneGroups)
        {
            if (sceneGroup != null && sceneGroup.Pointer == group.Pointer)
                return true;
        }

        return false;
    }

    private static Color GetUsernameColor(string username)
    {
        var hex = ExtractUsernameColorHex(username);
        return hex != null && ColorUtility.TryParseHtmlString($"#{hex}", out var color)
            ? color
            : Color.white;
    }
    
    internal void DestroyMapMarker()
    {
        try
        {
            if (mapMarker?.gameObject != null)
                Destroy(mapMarker.gameObject);
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"DestroyMapMarker failed for {model?.PlayerId}: {ex.Message}");
        }
        finally
        {
            mapMarker = null;
            mapMarkerSource = null;
        }
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
            }
            else
            {
                radarComponent!.enabled = sameSceneGroup;
                markerVisible = sameSceneGroup;
            }
        }
        
        if (mapMarker != null && mapMarkerSource != null)
        {
            mapMarkerSource._position = transform.position;

            var sceneGroup = ResolveSceneGroup();
            if (sceneGroup != null)
                mapMarkerSource._sceneGroup = sceneGroup;

            PositionMapMarker(null);

            if (PlayerMarkerTransforms.TryGetValue(ID, out var marker) && marker.markerArrow != null)
                marker.markerArrow.eulerAngles = new Vector3(0, 0, -transform.eulerAngles.y);
        }

        if (sameSceneGroup)
            UpdateDistanceLabel();
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