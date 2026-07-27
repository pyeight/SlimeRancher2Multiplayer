using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Events;

[PacketHandler((byte)PacketType.InitialEventsRaised, HandlerType.Client)]
internal sealed class InitialEventsRaisedHandler : BasePacketHandler<InitialEventsRaisedPacket>
{
    protected override bool Handle(InitialEventsRaisedPacket packet, IPEndPoint? _)
    {
        if (packet.Entries.Count == 0)
            return false;

        foreach (var entry in packet.Entries)
            NetworkEventManager.ApplyEventRaised(entry.EventKey, entry.DataKey);

        return false;
    }
}
