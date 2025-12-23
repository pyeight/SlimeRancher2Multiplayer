using System.Net;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.PlayerUpdate)]
public class PlayerUpdateHandler : BasePacketHandler
{
    public PlayerUpdateHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, IPEndPoint senderEndPoint)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<PlayerUpdatePacket>();

        // This is temporary :3
        if (packet.PlayerId == "HOST")
            return;

        playerManager.UpdatePlayer(
            packet.PlayerId,
            packet.Position,
            packet.Rotation,
            packet.HorizontalMovement,
            packet.ForwardMovement,
            packet.Yaw,
            packet.AirborneState,
            packet.Moving,
            packet.HorizontalSpeed,
            packet.ForwardSpeed,
            packet.Sprinting,
            packet.LookY
        );
        
        Main.Server.SendToAllExcept(packet, senderEndPoint);
    }
}