using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Drone;

[PacketHandler((byte)PacketType.DroneResourceUpdate)]
internal sealed class DroneResourceUpdateHandler : BasePacketHandler<DroneResourcePacket>
{
    protected override bool Handle(DroneResourcePacket packet, IPEndPoint? _)
    {
        HandlingPacket = true;

        try
        {
            var store = NetworkSceneManager.GetSceneGroup(packet.Scene)?.DiscoverableResources;
            if (store == null)
                return true;

            var data = NetworkDroneManager.ToResourceData(packet.Entry);
            store.UpdateResourceData(packet.Index, ref data);
        }
        finally
        {
            HandlingPacket = false;
        }

        return true;
    }
}
