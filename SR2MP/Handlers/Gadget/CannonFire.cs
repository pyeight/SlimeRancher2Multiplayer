using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Gadget;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Gadget;

[PacketHandler((byte)PacketType.CannonFire)]
internal sealed class CannonFireHandler : BasePacketHandler<CannonFirePacket>
{
    protected override bool Handle(CannonFirePacket packet, IPEndPoint? _)
    {
        HandlingPacket = true;
        try
        {
            NetworkGadgetManager.ApplyCannonFireTime(packet.GadgetId, packet.NextFireTime);
        }
        finally
        {
            HandlingPacket = false;
        }

        return true;
    }
}
