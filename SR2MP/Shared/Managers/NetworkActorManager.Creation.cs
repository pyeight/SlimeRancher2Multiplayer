using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Loading;
using SR2MP.Shared.Utils;

namespace SR2MP.Shared.Managers;

public sealed partial class NetworkActorManager
{
    public static InitialActorsPacket.ActorBase CreateInitialActor(IdentifiableModel actor)
    {
        if (actor.TryCast<SlimeModel>(out var slime))
            return CreateInitialSlime(slime);

        if (actor.TryCast<PlortModel>(out var plort))
            return CreateInitialPlort(plort);

        if (actor.TryCast<ProduceModel>(out var resource))
            return CreateInitialResource(resource);

        return CreateInitialActorBase(actor);
    }

    private static InitialActorsPacket.ActorBase CreateInitialActorBase(IdentifiableModel model) => new()
    {
        ActorId = model.actorId.Value,
        ActorTypeId = GetPersistentID(model.ident),
        Position = model.lastPosition,
        Rotation = model.TryCast<ActorModel>()?.lastRotation ?? Quaternion.identity,
        Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup)
    };

    private static InitialActorsPacket.Slime CreateInitialSlime(SlimeModel model) => new()
    {
        ActorId = model.actorId.Value,
        ActorTypeId = GetPersistentID(model.ident),
        Position = model.lastPosition,
        Rotation = model.lastRotation,
        Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup),
        Emotions = model.Emotions
    };

    private static InitialActorsPacket.Plort CreateInitialPlort(PlortModel model) => new()
    {
        ActorId = model.actorId.Value,
        ActorTypeId = GetPersistentID(model.ident),
        Position = model.lastPosition,
        Rotation = model.lastRotation,
        Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup),
        DestroyTime = model.destroyTime,
        Invulnerable = model._invulnerability?.IsInvulnerable ?? false,
        InvulnerablePeriod = model._invulnerability?.InvulnerabilityPeriod ?? 0f
    };

    private static InitialActorsPacket.Resource CreateInitialResource(ProduceModel model) => new()
    {
        ActorId = model.actorId.Value,
        ActorTypeId = GetPersistentID(model.ident),
        Position = model.lastPosition,
        Rotation = model.lastRotation,
        Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup),
        DestroyTime = model.destroyTime,
        ResourceState = model._state,
        ProgressTime = model.progressTime
    };
    
    public static ActorUpdateType DetermineUpdateTypeFromModel(ActorModel model)
    {
        if (model.TryCast<SlimeModel>() != null)
            return ActorUpdateType.Slime;
        if (model.TryCast<ProduceModel>() != null)
            return ActorUpdateType.Resource;
        if (model.TryCast<PlortModel>() != null)
            return ActorUpdateType.Plort;
        
        return ActorUpdateType.Actor;
    }
}