using System.Net;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;
using SR2MP.Shared.Managers;

namespace SR2MP.Shared.Handlers;

[PacketHandler((byte)PacketType.PlayerUpdate)]
public sealed class PlayerUpdateHandler : BaseSharedPacketHandler
{
    public PlayerUpdateHandler(NetworkManager networkManager, ClientManager clientManager) {}
    public PlayerUpdateHandler(Client.Client client, RemotePlayerManager playerManager) {}
    public override void Handle(byte[] data, IPEndPoint? clientEp = null)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<PlayerUpdatePacket>();

        // This is temporary :3
        // todo: change this
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