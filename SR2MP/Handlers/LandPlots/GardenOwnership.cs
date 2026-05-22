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
        if (NetworkGarden.Gardens.TryGetValue(packet.GardenID, out var garden))
            garden.LocallyOwned = false;

        return true;
    }
}