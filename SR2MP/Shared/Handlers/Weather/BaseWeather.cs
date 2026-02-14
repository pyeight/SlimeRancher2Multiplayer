using MelonLoader;
using SR2MP.Client.Managers;
using SR2MP.Packets.World;
using SR2MP.Shared.Handlers.Internal;
using System.Net;

namespace SR2MP.Shared.Handlers.Weather;

public abstract class BaseWeatherHandler : BasePacketHandler<WeatherPacket>
{
    private readonly bool _immediate;

    protected BaseWeatherHandler(bool isServerSide, bool immediate)
        : base(isServerSide) => _immediate = immediate;

    protected override sealed bool Handle(WeatherPacket packet, IPEndPoint? _)
    {
        MelonCoroutines.Start(NetworkWeatherManager.Apply(packet, _immediate));
        return false;
    }
}