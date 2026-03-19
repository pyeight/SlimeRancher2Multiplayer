using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using MelonLoader;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Actor;

[PacketHandler((byte)PacketType.InitialActors, HandlerType.Client)]
public sealed class ActorsLoadHandler : BasePacketHandler<InitialActorsPacket>
{
    protected override bool Handle(InitialActorsPacket packet, IPEndPoint? _)
    {
        actorManager.Actors.Clear();

        var toRemove = new CppCollections.Dictionary<ActorId, IdentifiableModel>(
            GameState.identifiables
                .Cast<CppCollections.IDictionary<ActorId, IdentifiableModel>>());

        handlingPacket = true;
        foreach (var (_, value) in toRemove)
        {
            if (value.ident.IsPlayer)
                continue;

            var gameObject = value.GetGameObject();

            if (gameObject)
                Destroyer.DestroyAny(gameObject, "SR2MP.InitialActors");
        }
        handlingPacket = false;

        GameState._actorIdProvider._nextActorId =
            packet.StartingActorID;
        GameState.world.worldTime = packet.WorldTime;

        foreach (var actor in packet.Actors)
        {
            actorManager.TrySpawnInitialActor(actor, out var _);
        }

        MelonCoroutines.Start(actorManager.TakeOwnershipOfNearby());

        return false;
    }
}