using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Shared.Handlers.Internal;
using System.Net;

namespace SR2MP.Shared.Handlers.Time;

[PacketHandler((byte)PacketType.FastForward)]
public sealed class BaseFastForwardHandler : BasePacketHandler<WorldTimePacket>
{
    protected override bool Handle(WorldTimePacket packet, IPEndPoint? _)
    {
        handlingPacket = true;
        SceneContext.Instance.TimeDirector.FastForwardTo(packet.Time);
        handlingPacket = false;
        return true;
    }
}