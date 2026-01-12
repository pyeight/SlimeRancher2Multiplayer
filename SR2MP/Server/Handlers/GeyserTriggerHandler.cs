using System.Net;
using SR2MP.Packets.Geyser;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.GeyserTrigger)]
public sealed class GeyserTriggerHandler : BasePacketHandler
{
    public GeyserTriggerHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, IPEndPoint clientEp)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<GeyserTriggerPacket>();

        var obj = GameObject.Find(packet.ObjectPath);

        handlingPacket = true;
        if (obj)
            obj.GetComponent<Geyser>().TriggerGeyser();
        handlingPacket = false;

        Main.Server.SendToAllExcept(packet, clientEp);
    }
}