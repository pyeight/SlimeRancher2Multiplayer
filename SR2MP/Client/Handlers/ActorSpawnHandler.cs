using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.ActorSpawn)]
public sealed class ActorSpawnHandler : BaseClientPacketHandler
{
    public ActorSpawnHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<ActorSpawnPacket>();

        actorManager.TrySpawnNetworkActor(packet.ActorId, packet.Position, packet.Rotation, packet.ActorType, packet.SceneGroup, out _);
    }
}