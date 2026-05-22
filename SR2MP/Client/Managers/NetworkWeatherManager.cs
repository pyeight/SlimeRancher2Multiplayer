using System.Collections;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Weather;
using Il2CppMonomiPark.SlimeRancher.World;
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

    internal static void Initialize()
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

    internal static IEnumerator Apply(WeatherPacket packet, bool immediate)
    {
        yield return new WaitFrames(3);
        HandlingPacket = true;

        var registry = Registry;
        var localDirector = Director;

        var zoneKeys = new List<ZoneDefinition>();
        foreach (var zone in registry._zones)
        {
            zoneKeys.Add(zone.Key);
            yield return null;
        }

        byte zoneId = 0;
        foreach (var zoneKey in zoneKeys)
        {
            if (!packet.Zones.TryGetValue(zoneId, out var data))
                continue;

            var zone = registry._zones[zoneKey];

            var forecastCopy = new List<WeatherModel.ForecastEntry>();
            foreach (var forecast in zone.Forecast)
                forecastCopy.Add(forecast);

            foreach (var forecast in forecastCopy)
            {
                yield return null;
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

                yield return new WaitFrames(2);
            }

            zone.Forecast.Clear();
            zone.Parameters.WindDirection = data.WindSpeed;

            foreach (var forecast in data.WeatherForecasts)
            {
                var pattern = WeatherUpdateHelper.GetPatternForZoneAndState(zoneKey, forecast.State.name);
                yield return null;

                zone.Forecast.Add(new WeatherModel.ForecastEntry
                {
                    State = forecast.State.Cast<IWeatherState>(),
                    Pattern = pattern,
                    Started = forecast.WeatherStarted,
                    StartTime = forecast.StartTime,
                    EndTime = forecast.EndTime
                });

                yield return new WaitFrames(2);
            }

            yield return null;
            zoneId++;
            yield return new WaitFrames(2);
        }

        if (!registry._zones.TryGetValue(localDirector.Zone, out var activeZone))
            yield break;

        var activeCopy = new List<WeatherModel.ForecastEntry>();
        foreach (var activeForecast in activeZone.Forecast)
        {
            activeCopy.Add(activeForecast);
            yield return null;
        }

        yield return null;

        foreach (var forecast in activeCopy)
        {
            yield return null;
            var patternInstance = registry.GetWeatherPatternInstance(
                localDirector.Zone,
                forecast.Pattern
            );

            yield return null;
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

            yield return new WaitFrames(3);
        }

        HandlingPacket = false;
    }
}