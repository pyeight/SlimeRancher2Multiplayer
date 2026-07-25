using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Gadget;

[PacketHandler((byte)PacketType.InitialGadgetLinks, HandlerType.Client)]
internal sealed class InitialGadgetLinksHandler : BasePacketHandler<InitialGadgetLinksPacket>
{
    protected override bool Handle(InitialGadgetLinksPacket packet, IPEndPoint? _)
    {
        foreach (var link in packet.Links)
            NetworkGadgetManager.ApplyGadgetLink(link.GadgetId, link.PartnerId);

        return false;
    }
}