using LiteNetLib;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.PlayerUpdate)]
public sealed class PlayerUpdateHandler : BasePacketHandler<PlayerUpdatePacket>
{
    public PlayerUpdateHandler(ClientManager clientManager)
        : base(clientManager) { }

    public override void Handle(PlayerUpdatePacket packet, NetPeer clientPeer)
    {
        // todo: This is temporary :3
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

        Main.Server.SendToAllExcept(packet, clientPeer);
    }
}