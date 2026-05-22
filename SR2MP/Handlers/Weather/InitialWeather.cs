using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Weather;

[PacketHandler((byte)PacketType.InitialWeather, HandlerType.Client)]
internal sealed class InitialWeatherHandler : BaseWeatherHandler
{
    public InitialWeatherHandler() : base(true) { }
}