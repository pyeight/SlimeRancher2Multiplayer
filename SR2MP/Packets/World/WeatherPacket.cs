using System.Collections;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Weather;
using SR2E.Utils;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World
{
    public class WeatherPacket : IPacket
    {
        public PacketType Type { get; set; }

        public NetworkWeatherModel model;

        public void Serialise(PacketWriter writer)
        {
            model.Write(writer);
        }

        public void Deserialise(PacketReader reader)
        {
            model = new NetworkWeatherModel();
            model.Read(reader);
        }

        public static IEnumerator CreateFromModel(
            WeatherModel model,
            PacketType type,
            System.Action<WeatherPacket> onComplete)
        {
            var packet = new WeatherPacket
            {
                Type = type,
                model = new NetworkWeatherModel
                {
                    Zones = new Dictionary<byte, WeatherZoneData>()
                }
            };

            byte zoneId = 0;

            foreach (var zone in model._zoneDatas)
            {
                var zoneData = new WeatherZoneData
                {
                    WeatherForecasts = new List<WeatherForecast>(),
                    WindSpeed = zone.Value.Parameters.WindDirection
                };

                foreach (var forecast in zone.Value.Forecast)
                {
                    if (!forecast.Started)
                        continue;

                    zoneData.WeatherForecasts.Add(new WeatherForecast
                    {
                        State = forecast.State.Cast<WeatherStateDefinition>(),
                        WeatherStarted = true,
                        StartTime = forecast.StartTime,
                        EndTime = forecast.EndTime
                    });
                }

                packet.model.Zones.Add(zoneId++, zoneData);
                yield return null;
            }

            onComplete?.Invoke(packet);
        }
    }

    public struct NetworkWeatherModel
    {
        public Dictionary<byte, WeatherZoneData> Zones;

        public void Write(PacketWriter writer)
        {
            writer.WriteInt(Zones.Count);
            foreach (var zone in Zones)
            {
                writer.WriteByte(zone.Key);
                zone.Value.Write(writer);
            }
        }

        public void Read(PacketReader reader)
        {
            Zones = new Dictionary<byte, WeatherZoneData>();
            var count = reader.ReadInt();

            for (int i = 0; i < count; i++)
            {
                var id = reader.ReadByte();
                var data = new WeatherZoneData();
                data.Read(reader);
                Zones.Add(id, data);
            }
        }
    }

    public struct WeatherForecast
    {
        public WeatherStateDefinition State;
        public bool WeatherStarted;
        public double StartTime;
        public double EndTime;

        public void Write(PacketWriter writer)
        {
            var stateName = State.name;

            int index = 0;
            var defs = LookupEUtil.weatherStateDefinitions;

            for (int i = 0; i < defs.Length; i++)
            {
                if (defs[i].name == stateName)
                {
                    index = i;
                    break;
                }
            }

            writer.WriteInt(index);
            writer.WriteBool(WeatherStarted);
            writer.WriteDouble(StartTime);
            writer.WriteDouble(EndTime);
        }

        public void Read(PacketReader reader)
        {
            State = LookupEUtil.weatherStateDefinitions[reader.ReadInt()];
            WeatherStarted = reader.ReadBool();
            StartTime = reader.ReadDouble();
            EndTime = reader.ReadDouble();
        }
    }

    public struct WeatherZoneData
    {
        public List<WeatherForecast> WeatherForecasts;
        public Vector3 WindSpeed;

        public void Write(PacketWriter writer)
        {
            writer.WriteInt(WeatherForecasts.Count);
            foreach (var f in WeatherForecasts)
                f.Write(writer);

            writer.WriteVector3(WindSpeed);
        }

        public void Read(PacketReader reader)
        {
            var count = reader.ReadInt();
            WeatherForecasts = new List<WeatherForecast>();

            for (int i = 0; i < count; i++)
            {
                var f = new WeatherForecast();
                f.Read(reader);
                WeatherForecasts.Add(f);
            }

            WindSpeed = reader.ReadVector3();
        }
    }
}
