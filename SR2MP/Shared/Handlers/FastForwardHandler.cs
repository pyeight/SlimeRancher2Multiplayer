using System.Net;
using SR2MP.Packets.Utils;

namespace SR2MP.Shared.Handlers;

[PacketHandler((byte)PacketType.FastForward)]
public sealed class FastForwardHandler : BaseSharedPacketHandler
{
    public override void Handle(byte[] data, IPEndPoint? clientEp = null)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<WorldTimePacket>();

        handlingPacket = true;
        SceneContext.Instance.TimeDirector.FastForwardTo(packet.Time);
        handlingPacket = false;

        
        if (clientEp != null)
        {
            Main.Server.SendToAllExcept(packet with
            {
                Type = (byte)PacketType.FastForward
            }, clientEp);
        }
    }
}