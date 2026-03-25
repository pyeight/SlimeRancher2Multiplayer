using System.Net;
using SR2MP.Components.LandPlots;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.LandPlots;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.LandPlots;

[PacketHandler((byte)PacketType.GardenUpdate)]
internal sealed class GardenUpdateHandler : BasePacketHandler<GardenUpdatePacket>
{
    protected override bool Handle(GardenUpdatePacket packet, IPEndPoint? _)
    {
        if (NetworkGarden.Gardens.TryGetValue(packet.GardenID, out var garden))
            garden.ApplyUpdate(packet.NextSpawnTime, packet.StoredWater, packet.NextSpawnRipens);

        return true;
    }
}