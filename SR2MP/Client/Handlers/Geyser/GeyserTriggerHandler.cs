using SR2MP.Client.Handlers.Internal;
using SR2MP.Packets.Geyser;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers.Geyser;

[PacketHandler((byte)PacketType.GeyserTrigger)]
public sealed class GeyserTriggerHandler : BaseClientPacketHandler<GeyserTriggerPacket>
{
    public GeyserTriggerHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(GeyserTriggerPacket packet)
    {
        var geyserObject = GameObject.Find(packet.ObjectPath);

        handlingPacket = true;
        if (geyserObject)
        {
            var geyser = geyserObject.GetComponent<Il2Cpp.Geyser>();
            geyser.StartCoroutine(geyser.RunGeyser(packet.Duration));
        }
        handlingPacket = false;
    }
}