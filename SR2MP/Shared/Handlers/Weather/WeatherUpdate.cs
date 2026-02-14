using SR2MP.Packets.Utils;

namespace SR2MP.Shared.Handlers.Weather;

[PacketHandler((byte)PacketType.WeatherUpdate, HandlerType.Client)]
public sealed class WeatherUpdateHandler : BaseWeatherHandler
{
    public WeatherUpdateHandler() : base(false) { }
}