using System.Net;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.MovementSound)]
public class MovementSoundHandler : BasePacketHandler
{
    public MovementSoundHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, IPEndPoint senderEndPoint)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<MovementSoundPacket>();

        fxManager.PlayTransientAudio(fxManager.allCues[packet.CueName], packet.Position);
        
        Main.Server.SendToAllExcept(packet, senderEndPoint);
    }
}