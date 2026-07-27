using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Gadget;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Gadget;

[PacketHandler((byte)PacketType.TeleporterLink)]
internal sealed class TeleporterLinkHandler : BasePacketHandler<TeleporterLinkPacket>
{
    protected override bool Handle(TeleporterLinkPacket packet, IPEndPoint? _)
    {
        StartCoroutine(NetworkGadgetManager.ApplyTeleporterLink(
            packet.SourceNodeId, packet.DestinationNodeId, packet.DestinationSceneGroup));
        
        return true;
    }
}
