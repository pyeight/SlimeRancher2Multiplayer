using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.LandPlots;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.LandPlots;

[PacketHandler((byte)PacketType.PlortCollection)]
internal sealed class PlortCollectionHandler : BasePacketHandler<PlortCollectionPacket>
{
    protected override bool Handle(PlortCollectionPacket packet, IPEndPoint? _)
    {
        if (!GameState.landPlots.TryGetValue(packet.ID, out var model))
            return true;

        var collector = model.gameObj ? model.gameObj.GetComponentInChildren<PlortCollector>() : null;
        if (!collector)
            return true;

        HandlingPacket = true;
        collector!._endCollectAt = packet.EndTime;
        collector.StartCollection();
        HandlingPacket = false;

        return true;
    }
}