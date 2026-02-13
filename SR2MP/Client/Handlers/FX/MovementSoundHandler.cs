using SR2MP.Client.Handlers.Internal;
using SR2MP.Packets.FX;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers.FX;

[PacketHandler((byte)PacketType.MovementSound)]
public sealed class MovementSoundHandler : BaseClientPacketHandler<MovementSoundPacket>
{
    public MovementSoundHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(MovementSoundPacket packet)
    {
        RemoteFXManager.PlayTransientAudio(fxManager.AllCues[packet.CueName], packet.Position, 0.8f);
    }
}