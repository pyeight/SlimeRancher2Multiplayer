using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;

namespace SR2MP.Handlers.Time;

[PacketHandler((byte)PacketType.FastForward)]
internal sealed class BaseFastForwardHandler : BasePacketHandler<FastForwardPacket>
{
    protected override bool Handle(FastForwardPacket packet, IPEndPoint? _)
    {
        var timeDirector = SceneContext.Instance.TimeDirector;
        
        if (packet.Time <= timeDirector._worldModel.worldTime)
            return true;

        HandlingPacket = true;
        timeDirector.FastForwardTo(packet.Time);
        HandlingPacket = false;
        return true;
    }
}