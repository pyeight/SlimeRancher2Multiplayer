using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.LandPlots;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.LandPlots;

[PacketHandler((byte)PacketType.AutoFeederSpeed)]
internal sealed class AutoFeederSpeedHandler : BasePacketHandler<AutoFeederSpeedPacket>
{
    protected override bool Handle(AutoFeederSpeedPacket packet, IPEndPoint? _)
    {
        var model = GameState.landPlots[packet.ID];
        var feeder = model.gameObj.GetComponentInChildren<SlimeFeeder>();

        HandlingPacket = true;
        feeder.SetFeederSpeed(packet.Speed);
        HandlingPacket = false;

        return true;
    }
}