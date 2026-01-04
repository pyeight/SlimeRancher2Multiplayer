using System.Net;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.PlayerUpdate)]
public sealed class PlayerUpdateHandler : BaseSharedPacketHandler
{
    public override void Handle(byte[] data, IPEndPoint? clientEp = null)
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

        
        if (clientEp != null)
            Main.Server.SendToAllExcept(packet, clientEp);
    }
}