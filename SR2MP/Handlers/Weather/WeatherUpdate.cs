using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Weather;

[PacketHandler((byte)PacketType.WeatherUpdate, HandlerType.Client)]
internal sealed class WeatherUpdateHandler : BaseWeatherHandler
{
    public WeatherUpdateHandler() : base(false) { }
}