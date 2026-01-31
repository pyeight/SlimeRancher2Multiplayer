using LiteNetLib;
using SR2MP.Packets.World;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.FastForward)]
public sealed class FastForwardHandler : BasePacketHandler<FastForwardPacket>
{
    public FastForwardHandler(ClientManager clientManager)
        : base(clientManager) { }

    public override void Handle(FastForwardPacket packet, NetPeer clientPeer)
    {
        handlingPacket = true;
        SceneContext.Instance.TimeDirector.FastForwardTo(packet.Time);
        handlingPacket = false;

        Main.Server.SendToAllExcept(packet, clientPeer);
    }
}
