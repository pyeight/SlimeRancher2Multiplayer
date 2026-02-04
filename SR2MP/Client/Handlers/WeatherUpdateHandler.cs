using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.WeatherUpdate)]
public sealed class BaseWeatherUpdateHandler : BaseWeatherHandler
{
    public BaseWeatherUpdateHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager, false)
    {
    }
}