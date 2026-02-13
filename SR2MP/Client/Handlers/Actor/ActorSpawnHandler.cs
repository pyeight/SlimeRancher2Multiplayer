using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Client.Handlers.Internal;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers.Actor;

[PacketHandler((byte)PacketType.ActorSpawn)]
public sealed class ActorSpawnHandler : BaseClientPacketHandler<ActorSpawnPacket>
{
    public ActorSpawnHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(ActorSpawnPacket packet)
    {
        if (actorManager.Actors.ContainsKey(packet.ActorId.Value))
        {
            SrLogger.LogPacketSize($"Actor {packet.ActorId.Value} already exists", SrLogTarget.Both);
            return;
        }

        actorManager.TrySpawnNetworkActor(packet.ActorId, packet.Position, packet.Rotation, packet.ActorType, packet.SceneGroup, out var actor);
        var slime = actor!.TryCast<SlimeModel>();
        if (slime != null)
            slime.Emotions = packet.Emotions;
    }
}