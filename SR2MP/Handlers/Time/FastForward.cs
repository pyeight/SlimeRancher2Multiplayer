using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;

namespace SR2MP.Handlers.Time;

[PacketHandler((byte)PacketType.FastForward)]
internal sealed class BaseFastForwardHandler : BasePacketHandler<WorldTimePacket>
{
    protected override bool Handle(WorldTimePacket packet, IPEndPoint? _)
    {
        HandlingPacket = true;
        SceneContext.Instance.TimeDirector.FastForwardTo(packet.Time);
        HandlingPacket = false;
        return true;
    }
}