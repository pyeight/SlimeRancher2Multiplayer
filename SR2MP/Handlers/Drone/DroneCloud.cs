using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Drone;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Drone;

[PacketHandler((byte)PacketType.DroneCloud)]
internal sealed class DroneCloudHandler : BasePacketHandler<DroneCloudPacket>
{
    protected override bool Handle(DroneCloudPacket packet, IPEndPoint? _)
    {
        if (!ActorManager.ActorTypes.TryGetValue(packet.TypeId, out var type) || type == null)
            return true;

        var cloud = GameState.droneModel.GetCloudModel();
        if (cloud == null)
            return true;

        HandlingPacket = true;
        cloud.SetAmount(type, packet.Amount);
        HandlingPacket = false;

        return true;
    }
}
