using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.LandPlots;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.LandPlots;

[PacketHandler((byte)PacketType.AutoFeederDispense)]
internal sealed class AutoFeederDispenseHandler : BasePacketHandler<AutoFeederDispensePacket>
{
    protected override bool Handle(AutoFeederDispensePacket packet, IPEndPoint? _)
    {
        var model = GameState.landPlots[packet.ID];
        var feeder = model.gameObj.GetComponentInChildren<SlimeFeeder>();

        feeder._nextEject = packet.NextTime;
        return true;
    }
}