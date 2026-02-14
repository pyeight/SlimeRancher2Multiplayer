using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Shared.Handlers.Internal;
using System.Net;

namespace SR2MP.Shared.Handlers.Time;

[PacketHandler((byte)PacketType.WorldTime, HandlerType.Client)]
public sealed class WorldTimeHandler : BasePacketHandler<WorldTimePacket>
{
    public WorldTimeHandler(bool isServerSide)
        : base(isServerSide) { }

    protected override bool Handle(WorldTimePacket packet, IPEndPoint? _)
    {
        SceneContext.Instance.TimeDirector._worldModel.worldTime = packet.Time;
        return false;
    }
}