using System.Collections;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Weather;
using Il2CppMonomiPark.SlimeRancher.World;
using MelonLoader;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

public abstract class BaseWeatherHandler : BaseClientPacketHandler<WeatherPacket>
{
    private bool _immediate;

    protected BaseWeatherHandler(Client client, RemotePlayerManager playerManager, bool immediate)
        : base(client, playerManager) => _immediate = immediate;

    protected override sealed void Handle(WeatherPacket packet) => MelonCoroutines.Start(Apply(packet, _immediate));

    private static IEnumerator Apply(WeatherPacket packet, bool immediate)
    {
        handlingPacket = true;

        var registry = Resources.FindObjectsOfTypeAll<WeatherRegistry>().FirstOrDefault();
        var director = Resources.FindObjectsOfTypeAll<WeatherDirector>().FirstOrDefault();

        var zoneKeys = new List<ZoneDefinition>();
        foreach (var zone in registry!._zones)
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
            for (var i = 0; i < zone.Forecast.Count; i++)
                forecastCopy.Add(zone.Forecast[i]);

            foreach (var forecast in forecastCopy)
            {
                var patternInstance = registry.GetWeatherPatternInstance(
                    zoneKey,
                    forecast.Pattern
                );

                if (patternInstance == null)
                {
                    director!.StopState(
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

                yield return null;
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

                yield return null;
            }

            zoneId++;
            yield return null;
        }

        if (!registry._zones.TryGetValue(director!.Zone, out var activeZone))
            yield break;

        var activeCopy = new List<WeatherModel.ForecastEntry>();
        for (var i = 0; i < activeZone.Forecast.Count; i++)
            activeCopy.Add(activeZone.Forecast[i]);

        foreach (var forecast in activeCopy)
        {
            var patternInstance = registry.GetWeatherPatternInstance(
                director.Zone,
                forecast.Pattern
            );

            if (patternInstance == null)
            {
                director.RunState(forecast.State.Cast<IWeatherState>(), activeZone.Parameters, immediate);
            }
            else
            {
                registry.RunPatternState(
                    director.Zone,
                    patternInstance,
                    forecast.State,
                    immediate
                );
            }

            yield return null;
        }

        handlingPacket = false;
    }
}