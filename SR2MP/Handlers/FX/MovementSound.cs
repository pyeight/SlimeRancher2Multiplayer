using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.FX;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.FX;

[PacketHandler((byte)PacketType.MovementSound)]
internal sealed class MovementSoundHandler : BasePacketHandler<MovementSoundPacket>
{
    protected override bool Handle(MovementSoundPacket packet, IPEndPoint? _)
    {
        RemoteFXManager.PlayTransientAudio(FXManager.AllCues[packet.CueName], packet.Position, IsServerSide ? 0.45f : 0.8f);
        return true;
    }
}