using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Loading;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.InitialActors)]
public sealed class ActorsLoadHandler : BaseClientPacketHandler<InitialActorsPacket>
{
    public ActorsLoadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(InitialActorsPacket packet)
    {
        actorManager.Actors.Clear();

        var toRemove = new CppCollections.Dictionary<ActorId, IdentifiableModel>(
            SceneContext.Instance.GameModel.identifiables
                .Cast<CppCollections.IDictionary<ActorId, IdentifiableModel>>());

        foreach (var actor in toRemove)
        {
            if (actor.value.ident.IsPlayer) continue;

            var gameObject = actor.value.GetGameObject();
            if (gameObject)
                Object.Destroy(gameObject);

            SceneContext.Instance.GameModel.DestroyIdentifiableModel(actor.value);
        }

        SceneContext.Instance.GameModel._actorIdProvider._nextActorId =
            packet.StartingActorID;

        foreach (var actor in packet.Actors)
        {
            if (!actorManager.TrySpawnNetworkActor(
                    new ActorId(actor.ActorId),
                    actor.Position,
                    actor.Rotation,
                    actor.ActorType,
                    actor.Scene,
                    out var spawnedActor))
                continue;

            /*if (spawnedActor!.TryGetNetworkComponent(out var component))
            {
                component.LocallyOwned = true;
            }*/
        }
    }
}
