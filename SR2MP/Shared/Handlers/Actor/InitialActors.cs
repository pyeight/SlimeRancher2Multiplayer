using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using MelonLoader;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;

namespace SR2MP.Shared.Handlers.Actor;

[PacketHandler((byte)PacketType.InitialActors, HandlerType.Client)]
public sealed class ActorsLoadHandler : BasePacketHandler<InitialActorsPacket>
{
    public ActorsLoadHandler(bool isServerSide) : base(isServerSide) { }

    protected override bool Handle(InitialActorsPacket packet, IPEndPoint? _)
    {
        actorManager.Actors.Clear();

        var toRemove = new CppCollections.Dictionary<ActorId, IdentifiableModel>(
            SceneContext.Instance.GameModel.identifiables
                .Cast<CppCollections.IDictionary<ActorId, IdentifiableModel>>());

        foreach (var (_, value) in toRemove)
        {
            if (value.ident.IsPlayer)
                continue;

            var gameObject = value.GetGameObject();

            if (gameObject)
                Object.Destroy(gameObject);

            SceneContext.Instance.GameModel.DestroyIdentifiableModel(value);
        }

        SceneContext.Instance.GameModel._actorIdProvider._nextActorId =
            packet.StartingActorID;
        SceneContext.Instance.GameModel.world.worldTime = packet.WorldTime;

        foreach (var actor in packet.Actors)
        {
            if (!actorManager.TrySpawnInitialActor(actor, out var spawnedActor))
                continue;

            // if (spawnedActor!.TryGetNetworkComponent(out var component))
            //     component.LocallyOwned = true;
        }

        MelonCoroutines.Start(actorManager.TakeOwnershipOfNearby());

        return false;
    }
}