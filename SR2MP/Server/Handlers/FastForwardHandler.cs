using System.Net;
using SR2MP.Packets.Time;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.FastForward)]
public sealed class FastForwardHandler : BasePacketHandler
{
    public FastForwardHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, IPEndPoint clientEp)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<WorldTimePacket>();

        handlingPacket = true;
        SceneContext.Instance.TimeDirector.FastForwardTo(packet.Time);
        handlingPacket = false;

        Main.Server.SendToAllExcept(packet with
        {
            Type = PacketType.BroadcastFastForward
        }, clientEp);
    }
}