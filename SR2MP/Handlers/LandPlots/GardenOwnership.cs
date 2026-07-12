using System.Net;
using SR2MP.Components.LandPlots;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.LandPlots;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.LandPlots;

[PacketHandler((byte)PacketType.GardenOwnership)]
internal sealed class GardenOwnershipHandler : BasePacketHandler<GardenOwnershipPacket>
{
    protected override bool Handle(GardenOwnershipPacket packet, IPEndPoint? _)
    {
        if (!NetworkGarden.Gardens.TryGetValue(packet.GardenID, out var garden))
            return true;

        if (string.IsNullOrEmpty(packet.ClaimerID))
        {
            if (!string.IsNullOrEmpty(garden.CurrentOwnerId) &&
                garden.CurrentOwnerId != packet.PreviousOwnerID)
                return true;
            
            if (!garden.IsHibernated)
                garden.ClaimOwnership();
        }
        else
        {
            garden.CurrentOwnerId = packet.ClaimerID;
            
            if (packet.ClaimerID != LocalID)
                garden.LocallyOwned = false;
        }

        return true;
    }
}