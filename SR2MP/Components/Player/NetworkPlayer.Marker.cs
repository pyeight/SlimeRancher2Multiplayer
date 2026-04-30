namespace SR2MP.Components.Player;

using Il2CppMonomiPark.SlimeRancher.UI;

internal partial class NetworkPlayer
{
    private void SetupMarker()
    {
        if (IsLocal)
            return;

        var markerComponent = gameObject.AddComponent<RadarTrackedPointOfInterest>();
        markerComponent.enabled = false;
        markerComponent._worldRadarPrefab = null;
        markerComponent._compassRadarPrefab = Instantiate(PlayerCompassPrefab);
        markerComponent._isOptional = false;
        markerComponent._overflowMode = RadarCompassOverflowMode.CLAMP;
        markerComponent._ranchBehaviour = RadarEntryRanchHandling.SHOW_IN_RANCH_AS_WELL;
        
        SrLogger.LogMessage($"Remote player marker added: {model!.PlayerId}");
    }

    private void UpdateMarker()
    {
        if (IsLocal) return;
        
        if (!PlayerMarkerTransforms.TryGetValue(ID, out var marker))
            return;
        
        if (!marker.mainMarker || !marker.markerArrow)
            return;
        
        marker.mainMarker!.localPosition = new Vector3(transform.position.x, transform.position.z, 0);
        marker.markerArrow!.eulerAngles = new Vector3(0, 0, -transform.eulerAngles.y);
    }
}