using System.Collections;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Weather;
using SR2MP.Client.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

internal sealed class WeatherPacket : IPacket
{
    public Dictionary<byte, WeatherZoneData> Zones;

    public PacketType Type { get; private init; }
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.Weather;

    public void Serialise(PacketWriter writer) => writer.WriteDictionary(Zones, PacketWriterDels.Byte, PacketWriterDels.NetObject<WeatherZoneData>.Writer);

    public void Deserialise(PacketReader reader) => Zones = reader.ReadDictionary(PacketReaderDels.Byte, PacketReaderDels.NetObject<WeatherZoneData>.Reader)!;

    public static IEnumerator CreateFromModel(
        WeatherModel model,
        PacketType type,
        Action<WeatherPacket>? onComplete)
    {
        var packet = new WeatherPacket
        {
            Type = type,
            Zones = new Dictionary<byte, WeatherZoneData>()
        };

        byte zoneId = 0;

        foreach (var zone in model._zoneDatas)
        {
            yield return null;
            var zoneData = new WeatherZoneData
            {
                WeatherForecasts = new List<WeatherForecast>(),
                WindSpeed = zone.Value.Parameters.WindDirection
            };
            foreach (var forecast in zone.Value.Forecast)
            {
                yield return null;
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

            packet.Zones.Add(zoneId++, zoneData);

            yield return new WaitFrames(3);
        }

        onComplete?.Invoke(packet);
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