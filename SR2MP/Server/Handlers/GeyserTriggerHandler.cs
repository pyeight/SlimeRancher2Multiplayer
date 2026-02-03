using System.Net;
using SR2MP.Packets.Geyser;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.GeyserTrigger)]
public sealed class GeyserTriggerHandler : BasePacketHandler<GeyserTriggerPacket>
{
    public GeyserTriggerHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(GeyserTriggerPacket packet, IPEndPoint clientEp)
    {
        var obj = GameObject.Find(packet.ObjectPath);

        handlingPacket = true;
        if (obj)
            obj.GetComponent<Geyser>().TriggerGeyser();
        handlingPacket = false;

        Main.Server.SendToAllExcept(packet, clientEp);
    }
}