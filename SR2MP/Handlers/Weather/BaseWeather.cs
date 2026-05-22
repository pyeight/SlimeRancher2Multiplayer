using System.Net;
using MelonLoader;
using SR2MP.Client.Managers;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.World;

namespace SR2MP.Handlers.Weather;

internal abstract class BaseWeatherHandler : BasePacketHandler<WeatherPacket>
{
    private readonly bool _immediate;

    protected BaseWeatherHandler(bool immediate) => _immediate = immediate;

    protected sealed override bool Handle(WeatherPacket packet, IPEndPoint? _)
    {
        StartCoroutine(NetworkWeatherManager.Apply(packet, _immediate));
        return false;
    }
}