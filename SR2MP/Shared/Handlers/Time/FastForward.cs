using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Shared.Handlers.Internal;
using System.Net;

namespace SR2MP.Shared.Handlers.Time;

public abstract class BaseFastForwardHandler : BasePacketHandler<WorldTimePacket>
{
    protected BaseFastForwardHandler(bool isServerSide)
        : base(isServerSide) { }

    protected static void HandleTime(double time)
    {
        handlingPacket = true;
        SceneContext.Instance.TimeDirector.FastForwardTo(time);
        handlingPacket = false;
    }
}

[PacketHandler((byte)PacketType.BroadcastFastForward, HandlerType.Client)]
public sealed class ClientFastForwardHandler : BaseFastForwardHandler
{
    public ClientFastForwardHandler(bool isServerSide)
        : base(isServerSide) { }

    protected override bool Handle(WorldTimePacket packet, IPEndPoint? _)
    {
        HandleTime(packet.Time);
        return true;
    }
}

[PacketHandler((byte)PacketType.FastForward, HandlerType.Server)]
public sealed class ServerFastForwardHandler : BaseFastForwardHandler
{
    public ServerFastForwardHandler(bool isServerSide)
        : base(isServerSide) { }

    protected override bool Handle(WorldTimePacket packet, IPEndPoint? clientEp)
    {
        HandleTime(packet.Time);
        PacketSender.SendPacket(packet with { Type = PacketType.BroadcastFastForward }, clientEp);
        return false;
    }
}