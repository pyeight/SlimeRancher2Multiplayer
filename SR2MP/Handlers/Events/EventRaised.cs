using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Events;

[PacketHandler((byte)PacketType.EventRaised)]
internal sealed class EventRaisedHandler : BasePacketHandler<EventRaisedPacket>
{
    protected override bool Handle(EventRaisedPacket packet, IPEndPoint? _)
    {
        NetworkEventManager.ApplyEventRaised(packet.EventKey, packet.DataKey);
        return true;
    }
}
