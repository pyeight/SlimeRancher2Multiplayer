using SR2MP.Packets.Geyser;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.GeyserTrigger)]
public sealed class GeyserTriggerHandler : BaseClientPacketHandler
{
    public GeyserTriggerHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<GeyserTriggerPacket>();

        var obj = GameObject.Find(packet.ObjectPath);

        handlingPacket = true;
        if (obj)
            obj.GetComponent<Geyser>().TriggerGeyser();
        handlingPacket = false;
    }
}