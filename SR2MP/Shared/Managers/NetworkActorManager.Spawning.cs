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
        var model = GameState.CreateGadgetModel(type.Cast<GadgetDefinition>(), actorId, scene, position);
        model.eulerRotation = rotation.eulerAngles;

        HandlingPacket = true;
        var gadget = GadgetDirector.InstantiateGadgetFromModel(model);
        HandlingPacket = false;

        gadget.transform.SetPositionAndRotation(position, rotation);

        identModel = model.TryCast<IdentifiableModel>();
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

        model = GameState.CreateActorModel(
            actorId,
            type,
            scene,
            position,
            rotation).TryCast<IdentifiableModel>();

        if (model == null)
            return false;

        GameState.identifiables[actorId] = model;
        if (GameState.identifiablesByIdent.TryGetValue(type, out var actors))
        {
            actors.Add(model);
        }
        else
        {
            actors = new CppCollections.List<IdentifiableModel>();
            actors.Add(model);
            GameState.identifiablesByIdent.Add(type, actors);
        }

        HandlingPacket = true;
        var actor = InstantiationHelpers.InstantiateActorFromModel(model.Cast<ActorModel>());
        HandlingPacket = false;

        if (!actor)
            return true;

        var networkComponent = actor.AddComponent<NetworkActor>();
        networkComponent.LocallyOwned = false;
        networkComponent.PreviousPosition = position;
        networkComponent.NextPosition = position;
        networkComponent.PreviousRotation = rotation;
        networkComponent.NextRotation = rotation;
        actor.transform.position = position;
        ActorManager.Actors[actorId.Value] = model;

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
        var model = GameState.CreateGadgetModel(type.Cast<GadgetDefinition>(), actorId, scene, position);
        model.eulerRotation = rotation.eulerAngles;

        identifiableModel = model.TryCast<IdentifiableModel>();

        HandlingPacket = true;
        var gadget = GadgetDirector.InstantiateGadgetFromModel(model);
        HandlingPacket = false;

        gadget.transform.SetPositionAndRotation(position, rotation);

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

        model = GameState.CreateActorModel(
            actorId,
            type,
            scene,
            position,
            rotation).TryCast<IdentifiableModel>();

        if (model == null)
            return false;

        GameState.identifiables[actorId] = model;
        if (GameState.identifiablesByIdent.TryGetValue(type, out var actors))
        {
            actors.Add(model);
        }
        else
        {
            actors = new CppCollections.List<IdentifiableModel>();
            actors.Add(model);
            GameState.identifiablesByIdent.Add(type, actors);
        }

        HandlingPacket = true;
        var actor = InstantiationHelpers.InstantiateActorFromModel(model.Cast<ActorModel>());
        HandlingPacket = false;

        if (!actor)
            return true;

        var networkComponent = actor.AddComponent<NetworkActor>();
        networkComponent.LocallyOwned = false;
        networkComponent.PreviousPosition = position;
        networkComponent.NextPosition = position;
        networkComponent.PreviousRotation = rotation;
        networkComponent.NextRotation = rotation;
        actor.transform.position = position;
        ActorManager.Actors[actorId.Value] = model;

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

        model = GameState.CreateSlimeActorModel(
            actorId,
            type.Cast<SlimeDefinition>(),
            scene,
            position,
            rotation).TryCast<IdentifiableModel>();

        if (model == null)
            return false;

        model.Cast<SlimeModel>().Emotions = emotions;

        GameState.identifiables[actorId] = model;
        if (GameState.identifiablesByIdent.TryGetValue(type, out var actors))
        {
            actors.Add(model);
        }
        else
        {
            actors = new CppCollections.List<IdentifiableModel>();
            actors.Add(model);
            GameState.identifiablesByIdent.Add(type, actors);
        }

        HandlingPacket = true;
        var actor = InstantiationHelpers.InstantiateActorFromModel(model.Cast<ActorModel>());
        HandlingPacket = false;

        if (!actor)
            return true;

        var networkComponent = actor.AddComponent<NetworkActor>();
        networkComponent.LocallyOwned = false;
        networkComponent.PreviousPosition = position;
        networkComponent.NextPosition = position;
        networkComponent.PreviousRotation = rotation;
        networkComponent.NextRotation = rotation;
        actor.transform.position = position;
        ActorManager.Actors[actorId.Value] = model;

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

        model = GameState.CreateActorModel(
            actorId,
            type,
            scene,
            position,
            rotation).TryCast<IdentifiableModel>();

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

        GameState.identifiables[actorId] = model;
        if (GameState.identifiablesByIdent.TryGetValue(type, out var actors))
        {
            actors.Add(model);
        }
        else
        {
            actors = new CppCollections.List<IdentifiableModel>();
            actors.Add(model);
            GameState.identifiablesByIdent.Add(type, actors);
        }

        HandlingPacket = true;
        var actor = InstantiationHelpers.InstantiateActorFromModel(model.Cast<ActorModel>());
        HandlingPacket = false;

        if (!actor)
            return true;

        var networkComponent = actor.AddComponent<NetworkActor>();
        networkComponent.LocallyOwned = false;
        networkComponent.PreviousPosition = position;
        networkComponent.NextPosition = position;
        networkComponent.PreviousRotation = rotation;
        networkComponent.NextRotation = rotation;
        actor.transform.position = position;
        ActorManager.Actors[actorId.Value] = model;

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

        model = GameState.CreateActorModel(
            actorId,
            type,
            scene,
            position,
            rotation).TryCast<IdentifiableModel>();

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
        produceModel.state = state;
        produceModel.progressTime = progress;

        GameState.identifiables[actorId] = model;
        if (GameState.identifiablesByIdent.TryGetValue(type, out var actors))
        {
            actors.Add(model);
        }
        else
        {
            actors = new CppCollections.List<IdentifiableModel>();
            actors.Add(model);
            GameState.identifiablesByIdent.Add(type, actors);
        }

        HandlingPacket = true;
        var actor = InstantiationHelpers.InstantiateActorFromModel(model.Cast<ActorModel>());
        HandlingPacket = false;

        if (!actor)
            return true;

        var networkComponent = actor.AddComponent<NetworkActor>();
        networkComponent.LocallyOwned = false;
        networkComponent.PreviousPosition = position;
        networkComponent.NextPosition = position;
        networkComponent.PreviousRotation = rotation;
        networkComponent.NextRotation = rotation;
        actor.transform.position = position;
        ActorManager.Actors[actorId.Value] = model;

        var cycle = actor.GetComponent<ResourceCycle>();

        if (actorData.JointIndex >= 0 && cycle != null)
        {
            Joint? targetJoint = null;

            if (!string.IsNullOrEmpty(actorData.PlotID))
            {
                if (GameState.landPlots.TryGetValue(actorData.PlotID, out var plotModel)
                    && plotModel.gameObj)
                {
                    var spawner = plotModel.gameObj.GetComponentInChildren<SpawnResource>();
                    if (spawner != null && actorData.JointIndex < spawner.SpawnJoints.Count)
                        targetJoint = spawner.SpawnJoints[actorData.JointIndex];
                }
            }
            else
            {
                foreach (var spawner in Object.FindObjectsOfType<SpawnResource>())
                {
                    if (Vector3.Distance(spawner.transform.position, actorData.SpawnerPosition) < 0.1f)
                    {
                        if (actorData.JointIndex < spawner.SpawnJoints.Count)
                            targetJoint = spawner.SpawnJoints[actorData.JointIndex];
                        break;
                    }
                }
            }

            if (targetJoint != null)
            {
                HandlingPacket = true;
                cycle.Attach(targetJoint);
                HandlingPacket = false;

                produceModel.state = state;
                produceModel.progressTime = progress;
            }
        }

        if (cycle != null)
        {
            if (state == ResourceCycle.State.UNRIPE)
            {
                actor.transform.localScale = cycle._defaultScale * 0.33f;
                if (cycle._vacuumable)
                    cycle._vacuumable.enabled = false;
            }
            else
            {
                networkComponent.SetResourceState(state, progress, true);
            }
        }

        return true;
    }
}