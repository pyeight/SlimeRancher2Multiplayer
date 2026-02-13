using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers.Weather;

[PacketHandler((byte)PacketType.InitialWeather)]
public sealed class InitialBaseWeatherHandler : BaseWeatherHandler
{
    public InitialBaseWeatherHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager, true)
    {
    }
}