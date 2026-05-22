using Il2CppMonomiPark.SlimeRancher.UI;
using SR2MP.Shared.Managers;

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

        SrLogger.LogPacketSize($"Remote player marker added: {model!.PlayerId}");
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