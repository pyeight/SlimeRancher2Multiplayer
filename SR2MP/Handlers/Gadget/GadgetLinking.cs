using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Gadget;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Gadget;

[PacketHandler((byte)PacketType.GadgetLinking)]
internal sealed class GadgetLinkingHandler : BasePacketHandler<GadgetLinkingPacket>
{
    protected override bool Handle(GadgetLinkingPacket packet, IPEndPoint? _)
    {
        NetworkActorManager.ApplyGadgetLink(packet.GadgetId, packet.PartnerId);
        return true;
    }
}