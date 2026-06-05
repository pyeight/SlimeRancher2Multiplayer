using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;

namespace SR2MP.Handlers.Time;

[PacketHandler((byte)PacketType.WorldTime, HandlerType.Client)]
internal sealed class WorldTimeHandler : BasePacketHandler<WorldTimePacket>
{
    protected override bool Handle(WorldTimePacket packet, IPEndPoint? _)
    {
        if (!IsInRanchHouse)
            SceneContext.Instance.TimeDirector._worldModel.worldTime = packet.Time;
        return false;
    }
}