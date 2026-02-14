using SR2MP.Packets.Utils;

namespace SR2MP.Shared.Handlers.Weather;

[PacketHandler((byte)PacketType.InitialWeather, HandlerType.Client)]
public sealed class InitialWeatherHandler : BaseWeatherHandler
{
    public InitialWeatherHandler() : base(true) { }
}