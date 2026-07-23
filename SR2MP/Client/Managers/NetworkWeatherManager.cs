using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Weather;
using Il2CppMonomiPark.SlimeRancher.World;
using Il2CppMonomiPark.SlimeRancher.UI.Map;
using SR2MP.Packets.World;
using SR2MP.Server.Managers;

namespace SR2MP.Client.Managers;

internal static class NetworkWeatherManager
{
    public static WeatherRegistry Registry => SceneContext.Instance.WeatherRegistry;

    public static WeatherDirector Director
    {
        get
        {
            if (!director)
            {
                director = Resources.FindObjectsOfTypeAll<WeatherDirector>().FirstOrDefault()!;
            }

            return director;
        }
    }

    public static LightningStrike Lightning
    {
        get
        {
            if (!lightning)
            {
                lightning = Resources.FindObjectsOfTypeAll<LightningStrike>().First(x => x.BlastPower < 2749f);
            }

            return lightning;
        }
    }

    private static LightningStrike lightning;
    private static WeatherDirector director;

    public static readonly Dictionary<int, WeatherStateDefinition> WeatherStates = new();

    private static void Initialize()
    {
        var refer = GameContext.Instance.AutoSaveDirector._saveReferenceTranslation;
        foreach (var state in refer._weatherStateTranslation.RawLookupDictionary)
        {
            WeatherStates.Add(refer.GetPersistenceId(state.value), state.value.TryCast<WeatherStateDefinition>()!);
        }
    }

    public static void CheckInitialized()
    {
        if (WeatherStates.Count == 0)
            Initialize();
    }

    public static int GetPersistentID(WeatherStateDefinition state)
        => GameContext.Instance.AutoSaveDirector._saveReferenceTranslation
            .GetPersistenceId(state.Cast<IWeatherState>());

    internal static void Apply(WeatherPacket packet, bool immediate)
    {
        if (SceneContext.Instance.WeatherRegistry == null)
        {
            SrLogger.LogDebug("NetworkWeatherManager.Apply: WeatherRegistry not ready, dropping packet");
            return;
        }

        WeatherUpdateHelper.EnsureLookupInitialized();
        HandlingPacket = true;

        try
        {
            var registry = Registry;
            var localDirector = Director;

            var zoneKeys = new List<ZoneDefinition>();
            foreach (var zone in registry._zones)
                zoneKeys.Add(zone.Key);

            foreach (var zoneKey in zoneKeys)
            {
                if (zoneKey == null || string.IsNullOrEmpty(zoneKey.name))
                    continue;

                if (!packet.Zones.TryGetValue(zoneKey.name, out var data))
                    continue;

                var zone = registry._zones[zoneKey];

                var forecastCopy = new List<WeatherModel.ForecastEntry>();
                foreach (var forecast in zone.Forecast)
                    forecastCopy.Add(forecast);

                foreach (var forecast in forecastCopy)
                {
                    var patternInstance = registry.GetWeatherPatternInstance(
                        zoneKey,
                        forecast.Pattern
                    );

                    if (patternInstance == null)
                    {
                        localDirector.StopState(
                            forecast.State.Cast<IWeatherState>(),
                            zone.Parameters
                        );
                    }
                    else
                    {
                        registry.StopPatternState(
                            zoneKey,
                            patternInstance,
                            forecast.State
                        );
                    }
                }

                zone.Forecast.Clear();
                zone.Parameters.WindDirection = data.WindSpeed;

                foreach (var forecast in data.WeatherForecasts)
                {
                    var pattern = WeatherUpdateHelper.GetPatternForZoneAndState(zoneKey, forecast.State.name);

                    zone.Forecast.Add(new WeatherModel.ForecastEntry
                    {
                        State = forecast.State.Cast<IWeatherState>(),
                        Pattern = pattern,
                        Started = forecast.WeatherStarted,
                        StartTime = forecast.StartTime,
                        EndTime = forecast.EndTime
                    });
                }
            }

            if (!registry._zones.TryGetValue(localDirector.Zone, out var activeZone))
                return;

            var activeCopy = new List<WeatherModel.ForecastEntry>();
            foreach (var activeForecast in activeZone.Forecast)
                activeCopy.Add(activeForecast);

            foreach (var forecast in activeCopy)
            {
                var patternInstance = registry.GetWeatherPatternInstance(
                    localDirector.Zone,
                    forecast.Pattern
                );

                if (patternInstance == null)
                {
                    localDirector.RunState(forecast.State.Cast<IWeatherState>(), activeZone.Parameters, immediate);
                }
                else
                {
                    registry.RunPatternState(
                        localDirector.Zone,
                        patternInstance,
                        forecast.State,
                        immediate
                    );
                }
            }

            if (ActiveMapUI != null)
            {
                var zoomedOutUI = ActiveMapUI._zoomedOutUI;
                if (zoomedOutUI != null && zoomedOutUI._zoneMarkerUIs != null)
                {
                    foreach (var markerUI in zoomedOutUI._zoneMarkerUIs)
                    {
                        var zoneMarkerUI = markerUI.TryCast<ZoneMarkerUI>();
                        if (zoneMarkerUI != null)
                        {
                            zoneMarkerUI.SetUpWeather();
                        }
                    }
                }
            }
        }
        finally
        {
            HandlingPacket = false;
        }
    }
}