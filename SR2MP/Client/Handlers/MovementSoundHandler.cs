using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.MovementSound)]
public class MovementSoundHandler : BaseClientPacketHandler
{
    public MovementSoundHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<MovementSoundPacket>();
        
        fxManager.PlayTransientAudio(fxManager.allCues[packet.CueName], packet.Position);
    }
}