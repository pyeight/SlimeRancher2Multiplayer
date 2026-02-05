using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2E.Utils;
using SR2MP.Components.Actor;
using SR2MP.Packets.Loading;

namespace SR2MP.Shared.Managers;

public sealed partial class NetworkActorManager
{
    public bool TrySpawnNetworkActor(ActorId actorId, Vector3 position, Quaternion rotation, int typeId, int sceneId, out ActorModel? actorModel)
    {
        actorModel = null;

        if (Main.RockPlortBug)
            typeId = 25;

        var scene = NetworkSceneManager.GetSceneGroup(sceneId);

        if (!ActorTypes.TryGetValue(typeId, out var type))
        {
            SrLogger.LogWarning($"Tried to spawn actor with an invalid type!\n\tActor {actorId.Value}: type_{typeId}");
            return false;
        }

        if (!type.prefab)
            return false;

        if (type.isGadget())
        {
            SrLogger.LogWarning($"Tried to spawn gadget over the network, this has not been implemented yet!\n\tActor {actorId.Value}: {type.name}");
            return false;
        }

        if (ActorIDAlreadyInUse(actorId))
            return false;

        actorModel = SceneContext.Instance.GameModel.CreateActorModel(
                actorId,
                type,
                scene,
                position,
                rotation);

        if (actorModel == null)
            return false;

        SceneContext.Instance.GameModel.identifiables[actorId] = actorModel;
        if (SceneContext.Instance.GameModel.identifiablesByIdent.TryGetValue(type, out var actors))
        {
            actors.Add(actorModel);
        }
        else
        {
            actors = new CppCollections.List<IdentifiableModel>();
            actors.Add(actorModel);
            SceneContext.Instance.GameModel.identifiablesByIdent.Add(type, actors);
        }

        handlingPacket = true;
        var actor = InstantiationHelpers.InstantiateActorFromModel(actorModel);
        handlingPacket = false;

        if (!actor)
            return true;
        var networkComponent = actor.AddComponent<NetworkActor>();
        networkComponent.previousPosition = position;
        networkComponent.nextPosition = position;
        networkComponent.previousRotation = rotation;
        networkComponent.nextRotation = rotation;
        actor.transform.position = position;
        actorManager.Actors[actorId.Value] = actorModel;

        return true;
    }

    public bool TrySpawnInitialActor(InitialActorsPacket.ActorBase actorData, out IdentifiableModel? model)
    {
        model = null;

        if (actorData is InitialActorsPacket.Slime slimeData)
            return TrySpawnInitialSlime(slimeData, out model);

        if (actorData is InitialActorsPacket.Plort plortData)
            return TrySpawnInitialPlort(plortData, out model);

        if (actorData is InitialActorsPacket.Resource resourceData)
            return TrySpawnInitialResource(resourceData, out model);

        var sceneId = actorData.Scene;
        var typeId = actorData.ActorTypeId;
        var actorId = new ActorId(actorData.ActorId);
        var position = actorData.Position;
        var rotation = actorData.Rotation;

        if (Main.RockPlortBug)
            typeId = 25;

        var scene = NetworkSceneManager.GetSceneGroup(sceneId);

        if (!ActorTypes.TryGetValue(typeId, out var type))
        {
            SrLogger.LogWarning($"Tried to spawn actor with an invalid type!\n\tActor {actorId.Value}: type_{typeId}");
            return false;
        }

        if (!type.prefab)
            return false;

        if (type.isGadget())
        {
            SrLogger.LogWarning($"Tried to spawn gadget over the network, this has not been implemented yet!\n\tActor {actorId.Value}: {type.name}");
            return false;
        }

        if (ActorIDAlreadyInUse(actorId))
            return false;

        model = SceneContext.Instance.GameModel.CreateActorModel(
                actorId,
                type,
                scene,
                position,
                rotation);

        if (model == null)
            return false;

        SceneContext.Instance.GameModel.identifiables[actorId] = model;
        if (SceneContext.Instance.GameModel.identifiablesByIdent.TryGetValue(type, out var actors))
        {
            actors.Add(model);
        }
        else
        {
            actors = new CppCollections.List<IdentifiableModel>();
            actors.Add(model);
            SceneContext.Instance.GameModel.identifiablesByIdent.Add(type, actors);
        }

        handlingPacket = true;
        var actor = InstantiationHelpers.InstantiateActorFromModel(model.Cast<ActorModel>());
        handlingPacket = false;

        if (!actor)
            return true;
        var networkComponent = actor.AddComponent<NetworkActor>();
        networkComponent.previousPosition = position;
        networkComponent.nextPosition = position;
        networkComponent.previousRotation = rotation;
        networkComponent.nextRotation = rotation;
        actor.transform.position = position;
        actorManager.Actors[actorId.Value] = model;

        return true;
    }

    private bool TrySpawnInitialSlime(InitialActorsPacket.Slime actorData, out IdentifiableModel? model)
    {
        model = null;

        var sceneId = actorData.Scene;
        var typeId = actorData.ActorTypeId;
        var actorId = new ActorId(actorData.ActorId);
        var position = actorData.Position;
        var rotation = actorData.Rotation;
        var emotions = actorData.Emotions;

        if (Main.RockPlortBug)
            typeId = 25;

        var scene = NetworkSceneManager.GetSceneGroup(sceneId);

        if (!ActorTypes.TryGetValue(typeId, out var type))
        {
            SrLogger.LogWarning($"Tried to spawn actor with an invalid type!\n\tActor {actorId.Value}: type_{typeId}");
            return false;
        }

        if (!type.prefab)
            return false;

        if (ActorIDAlreadyInUse(actorId))
            return false;

        model = SceneContext.Instance.GameModel.CreateSlimeActorModel(
                actorId,
                type.Cast<SlimeDefinition>(),
                scene,
                position,
                rotation);

        if (model == null)
            return false;

        model.Cast<SlimeModel>().Emotions = emotions;

        SceneContext.Instance.GameModel.identifiables[actorId] = model;
        if (SceneContext.Instance.GameModel.identifiablesByIdent.TryGetValue(type, out var actors))
        {
            actors.Add(model);
        }
        else
        {
            actors = new CppCollections.List<IdentifiableModel>();
            actors.Add(model);
            SceneContext.Instance.GameModel.identifiablesByIdent.Add(type, actors);
        }

        handlingPacket = true;
        var actor = InstantiationHelpers.InstantiateActorFromModel(model.Cast<ActorModel>());
        handlingPacket = false;

        if (!actor)
            return true;
        var networkComponent = actor.AddComponent<NetworkActor>();
        networkComponent.previousPosition = position;
        networkComponent.nextPosition = position;
        networkComponent.previousRotation = rotation;
        networkComponent.nextRotation = rotation;
        actor.transform.position = position;
        actorManager.Actors[actorId.Value] = model;

        return true;
    }
    private bool TrySpawnInitialPlort(InitialActorsPacket.Plort actorData, out IdentifiableModel? model)
    {
        model = null;

        var sceneId = actorData.Scene;
        var typeId = actorData.ActorTypeId;
        var actorId = new ActorId(actorData.ActorId);
        var position = actorData.Position;
        var rotation = actorData.Rotation;
        var destroyTime = actorData.DestroyTime;
        var invulnerable = actorData.Invulnerable;
        var invulnerablePeriod = actorData.InvulnerablePeriod;

        if (Main.RockPlortBug)
            typeId = 25;

        var scene = NetworkSceneManager.GetSceneGroup(sceneId);

        if (!ActorTypes.TryGetValue(typeId, out var type))
        {
            SrLogger.LogWarning($"Tried to spawn actor with an invalid type!\n\tActor {actorId.Value}: type_{typeId}");
            return false;
        }

        if (!type.prefab)
            return false;

        if (ActorIDAlreadyInUse(actorId))
            return false;

        model = SceneContext.Instance.GameModel.CreateActorModel(
                actorId,
                type,
                scene,
                position,
                rotation);

        if (model == null)
            return false;

        var plortModel = model.TryCast<PlortModel>();
        if (plortModel == null)
        {
            SrLogger.LogWarning(
                $"Plort Actor failed to initialize: Did not create a PlortModel successfully.\n\tActor ID: {actorId},\n\tIdentifiable Type: {type.name}");
            return false;
        }

        plortModel.destroyTime = destroyTime;
        plortModel._invulnerability.IsInvulnerable = invulnerable;
        plortModel._invulnerability.InvulnerabilityPeriod = invulnerablePeriod;

        SceneContext.Instance.GameModel.identifiables[actorId] = model;
        if (SceneContext.Instance.GameModel.identifiablesByIdent.TryGetValue(type, out var actors))
        {
            actors.Add(model);
        }
        else
        {
            actors = new CppCollections.List<IdentifiableModel>();
            actors.Add(model);
            SceneContext.Instance.GameModel.identifiablesByIdent.Add(type, actors);
        }

        handlingPacket = true;
        var actor = InstantiationHelpers.InstantiateActorFromModel(model.Cast<ActorModel>());
        handlingPacket = false;

        if (!actor)
            return true;
        var networkComponent = actor.AddComponent<NetworkActor>();
        networkComponent.previousPosition = position;
        networkComponent.nextPosition = position;
        networkComponent.previousRotation = rotation;
        networkComponent.nextRotation = rotation;
        actor.transform.position = position;
        actorManager.Actors[actorId.Value] = model;

        return true;
    }
    private bool TrySpawnInitialResource(InitialActorsPacket.Resource actorData, out IdentifiableModel? model)
    {
        model = null;

        var sceneId = actorData.Scene;
        var typeId = actorData.ActorTypeId;
        var actorId = new ActorId(actorData.ActorId);
        var position = actorData.Position;
        var rotation = actorData.Rotation;
        var destroyTime = actorData.DestroyTime;
        var state = actorData.ResourceState;
        var progress = actorData.ProgressTime;

        if (Main.RockPlortBug)
            typeId = 25;

        var scene = NetworkSceneManager.GetSceneGroup(sceneId);

        if (!ActorTypes.TryGetValue(typeId, out var type))
        {
            SrLogger.LogWarning($"Tried to spawn actor with an invalid type!\n\tActor {actorId.Value}\n\tIdentifiable Type: {typeId}");
            return false;
        }

        if (!type.prefab)
            return false;

        if (ActorIDAlreadyInUse(actorId))
            return false;

        model = SceneContext.Instance.GameModel.CreateActorModel(
                actorId,
                type,
                scene,
                position,
                rotation);

        if (model == null)
            return false;

        var produceModel = model.TryCast<ProduceModel>();
        if (produceModel == null)
        {
            SrLogger.LogWarning(
                $"Resource Actor failed to initialize: Did not create a ProduceModel successfully.\n\tActor ID: {actorId},\n\tIdentifiable Type: {type.name}");
            return false;
        }

        produceModel.destroyTime = destroyTime;
        produceModel.state = state;
        produceModel.progressTime = progress;

        SceneContext.Instance.GameModel.identifiables[actorId] = model;
        if (SceneContext.Instance.GameModel.identifiablesByIdent.TryGetValue(type, out var actors))
        {
            actors.Add(model);
        }
        else
        {
            actors = new CppCollections.List<IdentifiableModel>();
            actors.Add(model);
            SceneContext.Instance.GameModel.identifiablesByIdent.Add(type, actors);
        }

        handlingPacket = true;
        var actor = InstantiationHelpers.InstantiateActorFromModel(model.Cast<ActorModel>());
        handlingPacket = false;

        if (!actor)
            return true;
        var networkComponent = actor.AddComponent<NetworkActor>();
        networkComponent.previousPosition = position;
        networkComponent.nextPosition = position;
        networkComponent.previousRotation = rotation;
        networkComponent.nextRotation = rotation;
        actor.transform.position = position;
        actorManager.Actors[actorId.Value] = model;

        return true;
    }
}