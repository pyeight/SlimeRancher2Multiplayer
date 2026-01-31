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
    [PacketHandler((byte)PacketType.InitialWeather)]
    public sealed class InitialWeatherHandler : BaseClientPacketHandler<WeatherPacket>
    {
        public InitialWeatherHandler(Client client, RemotePlayerManager playerManager)
            : base(client, playerManager) { }

        public override void Handle(WeatherPacket packet)
        {
            MelonCoroutines.Start(Apply(packet));
        }

        private IEnumerator Apply(WeatherPacket packet)
        {
            handlingPacket = true;

            var registry = Resources.FindObjectsOfTypeAll<WeatherRegistry>().FirstOrDefault();
            var director = Resources.FindObjectsOfTypeAll<WeatherDirector>().FirstOrDefault();

            if (registry == null || director == null)
            {
                handlingPacket = false;
                yield break;
            }
            
            var zoneKeys = new List<ZoneDefinition>();
            foreach (var zone in registry._zones)
            {
                zoneKeys.Add(zone.Key);
                yield return null;
            }

            byte zoneId = 0;
            foreach (var zoneKey in zoneKeys)
            {
                if (!packet.Model.Zones.TryGetValue(zoneId++, out var data))
                    continue;

                var zone = registry._zones[zoneKey];
                
                var forecastCopy = new List<WeatherModel.ForecastEntry>();
                for (int i = 0; i < zone.Forecast.Count; i++)
                    forecastCopy.Add(zone.Forecast[i]);

                foreach (var forecast in forecastCopy)
                {
                    if (forecast.Started)
                    {
                        director.StopState(forecast.State.Cast<IWeatherState>(), zone.Parameters, true);
                        yield return null;
                    }
                }
                
                zone.Forecast.Clear();
                zone.Parameters.WindDirection = data.WindSpeed;

                foreach (var forecast in data.WeatherForecasts)
                {
                    zone.Forecast.Add(new WeatherModel.ForecastEntry
                    {
                        State = forecast.State.Cast<IWeatherState>(),
                        Started = forecast.WeatherStarted,
                        StartTime = forecast.StartTime,
                        EndTime = forecast.EndTime
                    });
                    
                    yield return null;
                }

                yield return null;
            }
            
            if (registry._zones.TryGetValue(director.Zone, out var activeZone))
            {
                var activeForecastCopy = new List<WeatherModel.ForecastEntry>();
                for (int i = 0; i < activeZone.Forecast.Count; i++)
                    activeForecastCopy.Add(activeZone.Forecast[i]);

                foreach (var forecast in activeForecastCopy)
                {
                    if (!forecast.Started)
                        continue;

                    director.RunState(forecast.State.Cast<IWeatherState>(), activeZone.Parameters, true);
                    yield return null;
                }
            }

            handlingPacket = false;
        }
    }
}
