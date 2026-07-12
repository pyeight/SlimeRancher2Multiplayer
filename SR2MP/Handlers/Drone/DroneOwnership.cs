using System.Net;
using SR2MP.Components.Drone;
using SR2MP.Handlers.Internal;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Drone;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Drone;

[PacketHandler((byte)PacketType.DroneOwnership)]
internal sealed class DroneOwnershipHandler : BasePacketHandler<DroneOwnershipPacket>
{
    protected override bool Handle(DroneOwnershipPacket packet, IPEndPoint? _)
    {
        NetworkDroneManager.CachedOwners[packet.StationId] = packet.ClaimerID ?? string.Empty;

        if (!NetworkDrone.Drones.TryGetValue(packet.StationId, out var drone) || !drone)
            return true;

        if (string.IsNullOrEmpty(packet.ClaimerID))
        {
            if (!string.IsNullOrEmpty(drone.CurrentOwnerId) && drone.CurrentOwnerId != packet.PreviousOwnerID)
                return true;
            
            if (!drone.IsHibernated)
                drone.ClaimOwnership();
        }
        else
        {
            drone.CurrentOwnerId = packet.ClaimerID;
            drone.LocallyOwned = packet.ClaimerID == LocalID;
        }

        return true;
    }
}
