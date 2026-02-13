using SR2MP.Client.Handlers.Internal;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers.Time;

[PacketHandler((byte)PacketType.BroadcastFastForward)]
public sealed class FastForwardHandler : BaseClientPacketHandler<WorldTimePacket>
{
    public FastForwardHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(WorldTimePacket packet)
    {
        handlingPacket = true;
        SceneContext.Instance.TimeDirector.FastForwardTo(packet.Time);
        handlingPacket = false;
    }
}