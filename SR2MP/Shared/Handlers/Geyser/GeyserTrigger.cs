using SR2MP.Packets.Geyser;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;
using System.Net;

namespace SR2MP.Shared.Handlers.Geyser;

[PacketHandler((byte)PacketType.GeyserTrigger)]
public sealed class GeyserTriggerHandler : BasePacketHandler<GeyserTriggerPacket>
{
    protected override bool Handle(GeyserTriggerPacket packet, IPEndPoint? _)
    {
        var geyserObject = GameObject.Find(packet.ObjectPath);

        handlingPacket = true;

        if (geyserObject)
        {
            var geyser = geyserObject.GetComponent<Il2Cpp.Geyser>();
            geyser.StartCoroutine(geyser.RunGeyser(packet.Duration));
        }

        handlingPacket = false;
        return true;
    }
}