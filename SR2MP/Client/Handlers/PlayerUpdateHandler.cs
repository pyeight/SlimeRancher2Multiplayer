using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.PlayerUpdate)]
public class PlayerUpdateHandler : BaseClientPacketHandler
{
    public PlayerUpdateHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<PlayerUpdatePacket>();

        // Don't update our own player
        if (packet.PlayerId == Client.OwnPlayerId)
            return;

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
            packet.LookY
        );
    }
}