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
        if (!GameState.landPlots.TryGetValue(packet.ID, out var model) || !model.gameObj)
            return true;

        var feeder = model.gameObj.GetComponentInChildren<SlimeFeeder>();
        if (!feeder)
            return true;

        feeder._nextEject = packet.NextTime;
        return true;
    }
}