using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.LandPlots;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.LandPlots;

[PacketHandler((byte)PacketType.PlortCollection)]
public sealed class PlortCollectionHandler : BasePacketHandler<PlortCollectionPacket>
{
    protected override bool Handle(PlortCollectionPacket packet, IPEndPoint? _)
    {
        var model = GameState.landPlots[packet.ID];
        var collector = model.gameObj.GetComponentInChildren<PlortCollector>();

        HandlingPacket =  true;
        collector._endCollectAt = packet.EndTime;
        collector.StartCollection();
        HandlingPacket = false;

        return true;
    }
}