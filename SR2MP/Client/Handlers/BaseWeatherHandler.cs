using MelonLoader;
using SR2MP.Client.Managers;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

public abstract class BaseWeatherHandler : BaseClientPacketHandler<WeatherPacket>
{
    private readonly bool _immediate;

    protected BaseWeatherHandler(Client client, RemotePlayerManager playerManager, bool immediate)
        : base(client, playerManager) => _immediate = immediate;

    protected override sealed void Handle(WeatherPacket packet) => MelonCoroutines.Start(NetworkWeatherManager.Apply(packet, _immediate));
}