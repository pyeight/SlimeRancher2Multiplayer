using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Drone;

[PacketHandler((byte)PacketType.InitialDroneResources, HandlerType.Client)]
internal sealed class InitialDroneResourcesHandler : BasePacketHandler<InitialDroneResourcesPacket>
{
    protected override bool Handle(InitialDroneResourcesPacket packet, IPEndPoint? _)
    {
        HandlingPacket = true;
        NetworkDroneManager.ApplyInitialResources(packet);
        HandlingPacket = false;

        return false;
    }
}
