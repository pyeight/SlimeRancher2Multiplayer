using System.Net;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.MovementSound)]
public sealed class MovementSoundHandler : BaseSharedPacketHandler
{
    public override void Handle(byte[] data, IPEndPoint? clientEp = null)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<MovementSoundPacket>();

        RemoteFXManager.PlayTransientAudio(fxManager.AllCues[packet.CueName], packet.Position, 0.45f);

        
        if (clientEp != null)
            Main.Server.SendToAllExcept(packet, clientEp);
    }
}