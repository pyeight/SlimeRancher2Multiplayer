using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Weather;
using SR2MP.Client.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

internal sealed class WeatherPacket : IPacket
{
    public Dictionary<string, WeatherZoneData> Zones;

    public PacketType Type { get; private init; }
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.Weather;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteDictionary(Zones, PacketWriterDels.String, (w, zone) =>
        {
            w.WriteList(zone.WeatherForecasts, (fw, forecast) =>
            {
                var id = NetworkWeatherManager.GetPersistentID(forecast.State);
                if (id == 0)
                    SrLogger.LogWarning($"WeatherForecast: GetPersistentID returned 0 for state {forecast.State?.name}");
                fw.WritePackedInt(id);
                fw.WriteBool(forecast.WeatherStarted);
                fw.WriteDouble(forecast.StartTime);
                fw.WriteDouble(forecast.EndTime);
            });
            w.WriteVector3(zone.WindSpeed);
        });
    }

    public void Deserialise(PacketReader reader)
    {
        NetworkWeatherManager.CheckInitialized();
        Zones = reader.ReadDictionary(PacketReaderDels.String, PacketReaderDels.NetObject<WeatherZoneData>.Reader)!;
    }

    public static WeatherPacket CreateFromModel(WeatherModel model, PacketType type)
    {
        var packet = new WeatherPacket
        {
            Type = type,
            Zones = new Dictionary<string, WeatherZoneData>()
        };

        foreach (var zone in model._zoneDatas)
        {
            if (zone.Key == null || string.IsNullOrEmpty(zone.Key.name))
                continue;

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

            packet.Zones[zone.Key.name] = zoneData;
        }

        return packet;
    }
}

internal sealed class WeatherZoneData : INetObject
{
    public List<WeatherForecast> WeatherForecasts;
    public Vector3 WindSpeed;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteList(WeatherForecasts, PacketWriterDels.NetObject<WeatherForecast>.Writer);
        writer.WriteVector3(WindSpeed);
    }

    public void Deserialise(PacketReader reader)
    {
        WeatherForecasts = reader.ReadList(PacketReaderDels.NetObject<WeatherForecast>.Reader)!;
        WindSpeed = reader.ReadVector3();
    }
}

internal sealed class WeatherForecast : INetObject
{
    public WeatherStateDefinition State;
    public bool WeatherStarted;
    public double StartTime;
    public double EndTime;

    public void Serialise(PacketWriter writer)
    {
        writer.WritePackedInt(NetworkWeatherManager.GetPersistentID(State));
        writer.WriteBool(WeatherStarted);
        writer.WriteDouble(StartTime);
        writer.WriteDouble(EndTime);
    }

    public void Deserialise(PacketReader reader)
    {
        NetworkWeatherManager.CheckInitialized();
        State = NetworkWeatherManager.WeatherStates[reader.ReadPackedInt()];
        WeatherStarted = reader.ReadBool();
        StartTime = reader.ReadDouble();
        EndTime = reader.ReadDouble();
    }
}