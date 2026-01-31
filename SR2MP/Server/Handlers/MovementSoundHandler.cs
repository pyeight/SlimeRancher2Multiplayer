using LiteNetLib;
using SR2MP.Packets.FX;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.MovementSound)]
public sealed class MovementSoundHandler : BasePacketHandler<MovementSoundPacket>
{
    public MovementSoundHandler(ClientManager clientManager)
        : base(clientManager) { }

    public override void Handle(MovementSoundPacket packet, NetPeer clientPeer)
    {
        RemoteFXManager.PlayTransientAudio(fxManager.AllCues[packet.CueName], packet.Position, 0.45f);

        Main.Server.SendToAllExcept(packet, clientPeer);
    }
}