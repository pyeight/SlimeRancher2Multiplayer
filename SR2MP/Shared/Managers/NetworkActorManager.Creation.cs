using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Loading;

namespace SR2MP.Shared.Managers;

public sealed partial class NetworkActorManager
{
    public InitialActorsPacket.ActorBase CreateInitialActor(IdentifiableModel actor)
    {
        var slime = actor.TryCast<SlimeModel>();
        var plort = actor.TryCast<PlortModel>();
        var resource = actor.TryCast<ProduceModel>();

        if (slime != null)
            return CreateInitialSlime(slime);
        if (plort != null)
            return CreateInitialPlort(plort);
        if (resource != null)
            return CreateInitialResource(resource);
        
        var model = actor.TryCast<ActorModel>();
        var rotation = model?.lastRotation ?? Quaternion.identity;
        var id = actor.actorId.Value;
        return new InitialActorsPacket.ActorBase
        {
            ActorId = id,
            ActorType = GetPersistentID(actor.ident),
            Position = actor.lastPosition,
            Rotation = rotation,
            Scene = NetworkSceneManager.GetPersistentID(actor.sceneGroup)
        };
    }

    private InitialActorsPacket.Slime CreateInitialSlime(SlimeModel model)
    {
        var rotation = model.lastRotation;
        var id = model.actorId.Value;
        return new InitialActorsPacket.Slime
        {
            ActorId = id,
            ActorType = GetPersistentID(model.ident),
            Position = model.lastPosition,
            Rotation = rotation,
            Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup),
            Emotions = model.Emotions
        };
    }
    private InitialActorsPacket.Plort CreateInitialPlort(PlortModel model)
    {
        var rotation = model.lastRotation;
        var id = model.actorId.Value;
        return new InitialActorsPacket.Plort
        {
            ActorId = id,
            ActorType = GetPersistentID(model.ident),
            Position = model.lastPosition,
            Rotation = rotation,
            Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup),
            DestroyTime = model.destroyTime,
            Invulnerable = model._invulnerability.IsInvulnerable,
            InvulnerablePeriod = model._invulnerability.InvulnerabilityPeriod
        };
    }
    private InitialActorsPacket.Resource CreateInitialResource(ProduceModel model)
    {
        var rotation = model.lastRotation;
        var id = model.actorId.Value;
        return new InitialActorsPacket.Resource
        {
            ActorId = id,
            ActorType = GetPersistentID(model.ident),
            Position = model.lastPosition,
            Rotation = rotation,
            Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup),
            DestroyTime = model.destroyTime,
            ResourceState = model._state,
            ProgressTime = model.progressTime
        };
    }
}