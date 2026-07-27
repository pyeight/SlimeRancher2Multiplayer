using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.LandPlots;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.LandPlots;

[PacketHandler((byte)PacketType.GardenOwnership)]
internal sealed class GardenOwnershipHandler : BasePacketHandler<GardenOwnershipPacket>
{
    protected override bool Handle(GardenOwnershipPacket packet, IPEndPoint? _)
    {
        foreach (var entry in packet.Entries)
            NetworkGardenManager.ApplyOwnership(entry.GardenId, entry.OwnerId);

        return true;
    }
}