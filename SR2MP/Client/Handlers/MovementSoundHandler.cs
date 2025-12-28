using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.MovementSound)]
public sealed class MovementSoundHandler : BaseClientPacketHandler
{
    public MovementSoundHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<MovementSoundPacket>();;

        RemoteFXManager.PlayTransientAudio(fxManager.AllCues[packet.CueName], packet.Position, 0.8f);
    }
}