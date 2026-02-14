using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;
using System.Net;

namespace SR2MP.Shared.Handlers.Player;

[PacketHandler((byte)PacketType.PlayerUpdate)]
public sealed class PlayerUpdateHandler : BasePacketHandler<PlayerUpdatePacket>
{
    public PlayerUpdateHandler(bool isServerSide)
        : base(isServerSide) { }

    protected override bool Handle(PlayerUpdatePacket packet, IPEndPoint? clientEp)
    {
        if (IsServerSide)
        {
            if (packet.PlayerId == "HOST")
                return false;
        }
        else if (packet.PlayerId == Main.Client.OwnPlayerId)
            return false;

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

        return true;
    }
}