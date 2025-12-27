using System.Net;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.MovementSound)]
public sealed class MovementSoundHandler : BasePacketHandler
{
    public MovementSoundHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, IPEndPoint clientEp)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<MovementSoundPacket>();

        RemoteFXManager.PlayTransientAudio(fxManager.AllCues[packet.CueName], packet.Position);

        Main.Server.SendToAllExcept(packet, clientEp);
    }
}