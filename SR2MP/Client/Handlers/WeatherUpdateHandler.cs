using System.Collections;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Weather;
using Il2CppMonomiPark.SlimeRancher.World;
using MelonLoader;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers
{
    [PacketHandler((byte)PacketType.WeatherUpdate)]
    public sealed class WeatherUpdateHandler : BaseClientPacketHandler<WeatherPacket>
    {
        public WeatherUpdateHandler(Client client, RemotePlayerManager playerManager)
            : base(client, playerManager)
        {
        }

        public override void Handle(WeatherPacket packet)
        {
            MelonCoroutines.Start(Apply(packet));
        }

        private IEnumerator Apply(WeatherPacket packet)
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
                packet.model.Zones.TryGetValue(zoneId, out var data);

                var zone = registry._zones[zoneKey];

                var forecastCopy = new List<WeatherModel.ForecastEntry>();
                for (int i = 0; i < zone.Forecast.Count; i++)
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

            registry._zones.TryGetValue(director!.Zone, out var activeZone);
            {
                var activeCopy = new List<WeatherModel.ForecastEntry>();
                for (int i = 0; i < activeZone.Forecast.Count; i++)
                    activeCopy.Add(activeZone.Forecast[i]);

                foreach (var forecast in activeCopy)
                {
                    var patternInstance = registry.GetWeatherPatternInstance(
                        director.Zone,
                        forecast.Pattern
                    );

                    if (patternInstance == null)
                    {
                        director.RunState(forecast.State.Cast<IWeatherState>(), activeZone.Parameters, false);
                    }
                    else
                    {
                        registry.RunPatternState(
                            director.Zone,
                            patternInstance,
                            forecast.State,
                            false
                        );
                    }

                    yield return null;
                }

                handlingPacket = false;
            }
        }
    }
}