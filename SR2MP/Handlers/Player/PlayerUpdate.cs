using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Player;

[PacketHandler((byte)PacketType.PlayerUpdate)]
internal sealed class PlayerUpdateHandler : BasePacketHandler<PlayerUpdatePacket>
{
    protected override bool Handle(PlayerUpdatePacket packet, IPEndPoint? clientEp)
    {
        if (packet.PlayerId == LocalID) return false;

        PlayerManager.UpdatePlayer(
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
            packet.LookY,
            packet.SceneGroup
        );

        return true;
    }
}