using SR2MP.Packets.FX;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

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