using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using MelonLoader;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Actor;

[PacketHandler((byte)PacketType.InitialActors, HandlerType.Client)]
internal sealed class ActorsLoadHandler : BasePacketHandler<InitialActorsPacket>
{
    protected override bool Handle(InitialActorsPacket packet, IPEndPoint? _)
    {
        ActorManager.Actors.Clear();

        var toRemove = new CppCollections.Dictionary<ActorId, IdentifiableModel>(
            GameState.identifiables
                .Cast<CppCollections.IDictionary<ActorId, IdentifiableModel>>());

        HandlingPacket = true;
        foreach (var (_, value) in toRemove)
        {
            if (value.ident.IsPlayer)
                continue;

            var gameObject = value.GetGameObject();

            if (gameObject)
                Destroyer.DestroyAny(gameObject, "SR2MP.InitialActors");
        }
        HandlingPacket = false;

        GameState._actorIdProvider._nextActorId =
            packet.StartingActorID;
        GameState.world.worldTime = packet.WorldTime;

        foreach (var actor in packet.Actors)
        {
            ActorManager.TrySpawnInitialActor(actor, out var _);
        }

        StartCoroutine(ActorManager.TakeOwnershipOfNearby());

        return false;
    }
}