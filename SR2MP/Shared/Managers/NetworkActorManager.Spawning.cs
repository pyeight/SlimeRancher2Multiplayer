using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2E.Utils;
using SR2MP.Components.Actor;
using SR2MP.Packets.Loading;

namespace SR2MP.Shared.Managers;

public sealed partial class NetworkActorManager
{
    private bool TrySpawnNetworkGadget(ActorId actorId, Vector3 position, Quaternion rotation, int typeId, int sceneId, out IdentifiableModel? identModel)
    {
        identModel = null;

        if (!ActorTypes.TryGetValue(typeId, out var type))
        {
            SrLogger.LogWarning($"Tried to spawn gadget with an invalid type!\n\tActor {actorId}: type_{typeId}");
            return false;
        }

        var scene = NetworkSceneManager.GetSceneGroup(sceneId);
        var model = SceneContext.Instance.GameModel.CreateGadgetModel(type.Cast<GadgetDefinition>(), actorId, scene, position);
        model.eulerRotation = rotation.ToEuler();
        
        handlingPacket = true;
        var gadget = GadgetDirector.InstantiateGadgetFromModel(model);
        handlingPacket = false;
        
        gadget.transform.rotation = rotation;
        
        identModel = model.Cast<IdentifiableModel>();
        return true;
    }
    public bool TrySpawnNetworkActor(ActorId actorId, Vector3 position, Quaternion rotation, int typeId, int sceneId, out IdentifiableModel? model)
    {
        model = null;

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
            return TrySpawnNetworkGadget(actorId, position, rotation, typeId, sceneId, out model);

        if (ActorIDAlreadyInUse(actorId))
            return false;

        model = SceneContext.Instance.GameModel.CreateActorModel(
                actorId,
                type,
                scene,
                position,
                rotation).Cast<IdentifiableModel>();

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
        networkComponent.LocallyOwned = false;
        networkComponent.previousPosition = position;
        networkComponent.nextPosition = position;
        networkComponent.previousRotation = rotation;
        networkComponent.nextRotation = rotation;
        actor.transform.position = position;
        actorManager.Actors[actorId.Value] = model;

        actor.GetComponent<ResourceCycle>()?.AttachToNearest();

        return true;
    }

    private bool TrySpawnInitialGadget(InitialActorsPacket.ActorBase actorData, out IdentifiableModel? identifiableModel)
    {
        identifiableModel = null;
        
        var sceneId = actorData.Scene;
        var actorId = new ActorId(actorData.ActorId);
        var position = actorData.Position;
        var rotation = actorData.Rotation;
        var typeId = actorData.ActorTypeId;

        if (!ActorTypes.TryGetValue(typeId, out var type))
        {
            SrLogger.LogWarning($"Tried to spawn actor with an invalid type!\n\tActor {actorData.ActorId}: type_{typeId}");
            return false;
        }

        var scene = NetworkSceneManager.GetSceneGroup(sceneId);
        var model = SceneContext.Instance.GameModel.CreateGadgetModel(type.Cast<GadgetDefinition>(), actorId, scene, position);
        model.eulerRotation = Quaternion.ToEulerAngles(rotation);

        identifiableModel = model.Cast<IdentifiableModel>();
        
        handlingPacket = true;
        var gadget = GadgetDirector.InstantiateGadgetFromModel(model);
        handlingPacket = false;
        
        gadget.transform.rotation = rotation;
        
        return true;
    }
    
    public bool TrySpawnInitialActor(InitialActorsPacket.ActorBase actorData, out IdentifiableModel? model)
    {
        model = null;

        var typeId = actorData.ActorTypeId;
        
        if (Main.RockPlortBug)
            typeId = 25;

        if (!ActorTypes.TryGetValue(typeId, out var type))
        {
            SrLogger.LogWarning($"Tried to spawn actor with an invalid type!\n\tActor {actorData.ActorId}: type_{typeId}");
            return false;
        }
        
        if (type.isGadget())
            return TrySpawnInitialGadget(actorData, out model);
        
        switch (actorData)
        {
            case InitialActorsPacket.Slime slimeData:
                return TrySpawnInitialSlime(slimeData, out model);
            case InitialActorsPacket.Plort plortData:
                return TrySpawnInitialPlort(plortData, out model);
            case InitialActorsPacket.Resource resourceData:
                return TrySpawnInitialResource(resourceData, out model);
        }

        var sceneId = actorData.Scene;
        var actorId = new ActorId(actorData.ActorId);
        var position = actorData.Position;
        var rotation = actorData.Rotation;

        var scene = NetworkSceneManager.GetSceneGroup(sceneId);

        if (!type.prefab)
            return false;

        if (type.isGadget())
        {
            SrLogger.LogWarning($"Tried to spawn gadget over the network, but used the non-gadget function!\n\tActor {actorId.Value}: {type.name}");
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
        networkComponent.LocallyOwned = false;
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
        networkComponent.LocallyOwned = false;
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
        networkComponent.LocallyOwned = false;
        networkComponent.previousPosition = position;
        networkComponent.nextPosition = position;
        networkComponent.previousRotation = rotation;
        networkComponent.nextRotation = rotation;
        actor.transform.position = position;
        actorManager.Actors[actorId.Value] = model;

        var plortInvulnerability = actor.GetComponent<PlortInvulnerability>();
        if (plortInvulnerability)
        {
            plortInvulnerability.IsInvulnerable = invulnerable;
            plortInvulnerability.InvulnerabilityPeriod = invulnerablePeriod;
        }

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
        {
            SrLogger.LogWarning(
                $"Resource Actor failed to initialize: Did not create any models successfully.\n\tActor ID: {actorId},\n\tIdentifiable Type: {type.name}");
            return false;
        }

        var produceModel = model.TryCast<ProduceModel>();
        if (produceModel == null)
        {
            SrLogger.LogWarning(
                $"Resource Actor failed to initialize: Did not create a ProduceModel successfully.\n\tActor ID: {actorId},\n\tIdentifiable Type: {type.name}");
            return false;
        }

        produceModel.destroyTime = destroyTime;

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
        networkComponent.LocallyOwned = false;
        networkComponent.previousPosition = position;
        networkComponent.nextPosition = position;
        networkComponent.previousRotation = rotation;
        networkComponent.nextRotation = rotation;
        actor.transform.position = position;
        actorManager.Actors[actorId.Value] = model;

        networkComponent.SetResourceState(state, progress);
        actor.GetComponent<ResourceCycle>()?.AttachToNearest();

        return true;
    }
}