using System.Net;
using SR2MP.Components.Drone;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Drone;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Drone;

[PacketHandler((byte)PacketType.DroneUpdate)]
internal sealed class DroneUpdateHandler : BasePacketHandler<DroneUpdatePacket>
{
    protected override bool Handle(DroneUpdatePacket packet, IPEndPoint? _)
    {
        if (NetworkDrone.Drones.TryGetValue(packet.StationId, out var drone) && drone)
            drone.ApplyUpdate(packet);

        return true;
    }
}
