using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Geyser;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Geyser;

[PacketHandler((byte)PacketType.GeyserTrigger)]
internal sealed class GeyserTriggerHandler : BasePacketHandler<GeyserTriggerPacket>
{
    protected override bool Handle(GeyserTriggerPacket packet, IPEndPoint? _)
    {
        var geyserObject = GameObject.Find(packet.ObjectPath);

        HandlingPacket = true;

        if (geyserObject)
        {
            var geyser = geyserObject.GetComponent<Il2Cpp.Geyser>();
            geyser.StartCoroutine(geyser.RunGeyser(packet.Duration));
        }

        HandlingPacket = false;
        return true;
    }
}