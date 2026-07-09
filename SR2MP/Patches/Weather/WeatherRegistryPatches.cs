using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Weather;
using Il2CppMonomiPark.SlimeRancher.World;
using SR2MP.Server.Managers;

namespace SR2MP.Patches.Weather;

[HarmonyPatch(typeof(WeatherRegistry))]
internal static class WeatherRegistryPatches
{
    [HarmonyPatch(nameof(WeatherRegistry.Update)), HarmonyPrefix]
    public static bool UpdatePrefix() => !Main.Client.IsConnected || Main.Server.IsRunning;

    [HarmonyPatch(nameof(WeatherRegistry.RunPatternState)), HarmonyPrefix]
    public static bool RunPatternStatePrefix()
    {
        WeatherUpdateHelper.EnsureLookupInitialized();
        return !Main.Client.IsConnected || Main.Server.IsRunning || HandlingPacket;
    }

    [HarmonyPatch(nameof(WeatherRegistry.StopPatternState)), HarmonyPrefix]
    public static bool StopPatternStatePrefix(WeatherRegistry __instance, ZoneDefinition zone)
    {
        WeatherUpdateHelper.EnsureLookupInitialized();

        if (!zone)
            return false;

        return !Main.Client.IsConnected || Main.Server.IsRunning || HandlingPacket;
    }

    [HarmonyPatch(nameof(WeatherRegistry.CalculateZoneMapData)), HarmonyPostfix]
    public static void CalculateZoneMapDataPostfix(WeatherRegistry __instance, ZoneMapData zoneMapData, CppCollections.List<ZoneWeatherMapData> mapData)
    {
        if (mapData == null)
            return;

        if (mapData.Count > 0)
            return;

        var zoneDef = zoneMapData.WeatherZone != null ? zoneMapData.WeatherZone : zoneMapData.PrimaryZone;
        if (zoneDef == null)
            return;

        if (!__instance._zones.TryGetValue(zoneDef, out var zoneWeatherData))
            return;

        var forecastList = zoneWeatherData.Forecast;
        if (forecastList == null)
            return;

        foreach (var forecast in forecastList)
        {
            if (forecast.Pattern == null || forecast.Pattern.Metadata == null)
                continue;
            
            var present = false;
            foreach (var existing in mapData)
            {
                if (existing.Metadata != forecast.Pattern.Metadata)
                    continue;

                present = true;
                break;
            }

            if (present)
                continue;

            var mapDataEntry = new ZoneWeatherMapData();
            
            mapDataEntry.Metadata = forecast.Pattern.Metadata;

            var stateDef = forecast.State.TryCast<WeatherStateDefinition>();
            mapDataEntry.MapTier = stateDef != null ? stateDef.MapTier : forecast.State.GetMapTier();

            mapData.Add(mapDataEntry);
        }
    }
}