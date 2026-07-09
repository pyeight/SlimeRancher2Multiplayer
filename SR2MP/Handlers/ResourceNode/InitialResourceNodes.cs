using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.World;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.ResourceNode;

[PacketHandler((byte)PacketType.InitialResourceNodes, HandlerType.Client)]
internal sealed class InitialResourceNodesHandler : BasePacketHandler<InitialResourceNodesPacket>
{
    protected override bool Handle(InitialResourceNodesPacket packet, IPEndPoint? _)
    {
        ResourceNodeManager.ApplyInitialPlacements(packet.Nodes);
        return true;
    }
}