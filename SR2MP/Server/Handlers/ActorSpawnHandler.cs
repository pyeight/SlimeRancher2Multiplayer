using LiteNetLib;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.ActorSpawn)]
public sealed class ActorSpawnHandler : BasePacketHandler<ActorSpawnPacket>
{
    public ActorSpawnHandler(ClientManager clientManager)
        : base(clientManager) { }

    public override void Handle(ActorSpawnPacket packet, NetPeer clientPeer)
    {
        actorManager.TrySpawnNetworkActor(packet.ActorId, packet.Position, packet.Rotation, packet.ActorType, packet.SceneGroup, out _);

        Main.Server.SendToAllExcept(packet, clientPeer);
    }
}