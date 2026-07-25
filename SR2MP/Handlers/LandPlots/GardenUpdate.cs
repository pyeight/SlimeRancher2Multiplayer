using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.LandPlots;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.LandPlots;

[PacketHandler((byte)PacketType.GardenUpdate)]
internal sealed class GardenUpdateHandler : BasePacketHandler<GardenUpdatePacket>
{
    protected override bool Handle(GardenUpdatePacket packet, IPEndPoint? _)
    {
        foreach (var entry in packet.Entries)
            NetworkGardenManager.ApplyState(entry);

        return true;
    }
}