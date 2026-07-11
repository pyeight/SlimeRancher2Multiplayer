using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Drone;
using Il2CppMonomiPark.SlimeRancher.Player;
using SR2MP.Components.Actor;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Loading;
using SR2MP.Shared.Utils;
using Unity.Mathematics;

namespace SR2MP.Shared.Managers;

internal sealed partial class NetworkActorManager
{
    public bool TrySpawnNetworkActor(
        ActorId actorId, Vector3 position, Quaternion rotation, int typeId, int sceneId, out IdentifiableModel? model,
        SlimeAppearance.AppearanceSaveSet firstAppearance = SlimeAppearance.AppearanceSaveSet.NONE,
        SlimeAppearance.AppearanceSaveSet secondAppearance = SlimeAppearance.AppearanceSaveSet.NONE,
        float4 emotions = default, bool sleeping = false)
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
        
        if (type.IsGadget())
            return TrySpawnNetworkGadget(actorId, position, rotation, typeId, sceneId, out model);
        
        if (ActorIDAlreadyInUse(actorId))
            return false;
        
        model = GameState.CreateActorModel(actorId, type, scene, position, rotation).TryCast<IdentifiableModel>();
        
        if (model == null)
            return false;
        
        var slimeModel = model.TryCast<SlimeModel>();
        if (slimeModel != null)
        {
            slimeModel.firstAppearanceSaveSet = firstAppearance;
            slimeModel.secondAppearanceSaveSet = secondAppearance;
            slimeModel.Emotions = emotions;
            slimeModel.isSleeping = sleeping;
        }

        GameState.identifiables[actorId] = model;
        if (GameState.identifiablesByIdent.TryGetValue(type, out var actors))
            actors.Add(model);
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
        networkComponent.previousPosition = position;
        networkComponent.nextPosition = position;
        networkComponent.previousRotation = rotation;
        networkComponent.nextRotation = rotation;
        actor.transform.position = position;
        ActorManager.Actors[actorId.Value] = model;
        
        if (slimeModel != null)
            StartCoroutine(EnsureSlimeAppearance(slimeModel));

        actor.GetComponent<ResourceCycle>()?.AttachToNearest();
        
        return true;
    }
    
    private bool TrySpawnNetworkGadget(ActorId actorId, Vector3 position, Quaternion rotation, int typeId, int sceneId, out IdentifiableModel? identModel)
    {
        identModel = null;
        
        if (!ActorTypes.TryGetValue(typeId, out var type))
        {
            SrLogger.LogWarning($"Tried to spawn gadget with an invalid type!\n\tActor {actorId}: type_{typeId}");
            return false;
        }
        
        RemoveExistingGadgetModel(actorId);

        var scene = NetworkSceneManager.GetSceneGroup(sceneId);
        var model = GameState.CreateGadgetModel(type.Cast<GadgetDefinition>(), actorId, scene, position, false);
        model.eulerRotation = rotation.eulerAngles;
        
        HandlingPacket = true;
        var gadget = GadgetDirector.InstantiateGadgetFromModel(model);
        HandlingPacket = false;
        
        gadget.transform.SetPositionAndRotation(position, rotation);
        
        var stationModel = model.TryCast<DroneStationGadgetModel>();
        if (stationModel != null)
            StartCoroutine(NetworkDroneManager.EnsureStationDrone(stationModel));

        identModel = model.TryCast<IdentifiableModel>();
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
        
        if (type.IsGadget())
            return TrySpawnInitialGadget(actorData, out model);
        
        switch (actorData)
        {
            case InitialActorsPacket.Slime slimeData:
                return TrySpawnInitialSlime(slimeData, out model);
            case InitialActorsPacket.Plort plortData:
                return TrySpawnInitialPlort(plortData, out model);
            case InitialActorsPacket.Resource resourceData:
                return TrySpawnInitialResource(resourceData, out model);
            case InitialActorsPacket.Sprinkle sprinkleData:
                return TrySpawnInitialSprinkle(sprinkleData, out model);
        }
        
        var sceneId = actorData.Scene;
        var actorId = new ActorId(actorData.ActorId);
        var position = actorData.Position;
        var rotation = actorData.Rotation;
        var scene = NetworkSceneManager.GetSceneGroup(sceneId);
        
        if (!type.prefab)
            return false;
        
        if (type.IsGadget())
        {
            SrLogger.LogWarning($"Tried to spawn gadget over the network, but used the non-gadget function!\n\tActor {actorId.Value}: {type.name}");
            return false;
        }
        
        if (ActorIDAlreadyInUse(actorId))
            return false;
        
        model = GameState.CreateActorModel(actorId, type, scene, position, rotation).TryCast<IdentifiableModel>();
        
        if (model == null)
            return false;
        
        GameState.identifiables[actorId] = model;
        if (GameState.identifiablesByIdent.TryGetValue(type, out var actors))
            actors.Add(model);
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
        networkComponent.previousPosition = position;
        networkComponent.nextPosition = position;
        networkComponent.previousRotation = rotation;
        networkComponent.nextRotation = rotation;
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
        
        model = GameState.CreateSlimeActorModel(actorId, type.Cast<SlimeDefinition>(), scene, position, rotation).TryCast<IdentifiableModel>();
        
        if (model == null)
            return false;
        
        var slime = model.Cast<SlimeModel>();
        slime.Emotions = emotions;
        slime.isSleeping = actorData.Sleeping;
        slime.firstAppearanceSaveSet = actorData.FirstAppearance;
        slime.secondAppearanceSaveSet = actorData.SecondAppearance;
        
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
        networkComponent.previousPosition = position;
        networkComponent.nextPosition = position;
        networkComponent.previousRotation = rotation;
        networkComponent.nextRotation = rotation;
        actor.transform.position = position;
        ActorManager.Actors[actorId.Value] = model;
        
        if (actorData.Radiancy != (int)ActorAppearanceType.Default)
            ApplyRadiancy(slime, (ActorAppearanceType)actorData.Radiancy);
        
        StartCoroutine(EnsureSlimeAppearance(slime));

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
        
        model = GameState.CreateActorModel(actorId, type, scene, position, rotation).TryCast<IdentifiableModel>();
        
        if (model == null)
            return false;
        
        var plortModel = model.TryCast<PlortModel>();
        if (plortModel == null)
        {
            SrLogger.LogWarning($"Plort Actor failed to initialize: Did not create a PlortModel successfully.\n\tActor ID: {actorId},\n\tIdentifiable Type: {type.name}");
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
        networkComponent.previousPosition = position;
        networkComponent.nextPosition = position;
        networkComponent.previousRotation = rotation;
        networkComponent.nextRotation = rotation;
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
        
        model = GameState.CreateActorModel(actorId, type, scene, position, rotation).TryCast<IdentifiableModel>();
        
        if (model == null)
        {
            SrLogger.LogWarning($"Resource Actor failed to initialize: Did not create any models successfully.\n\tActor ID: {actorId},\n\tIdentifiable Type: {type.name}");
            return false;
        }
        
        var produceModel = model.TryCast<ProduceModel>();
        if (produceModel == null)
        {
            SrLogger.LogWarning($"Resource Actor failed to initialize: Did not create a ProduceModel successfully.\n\tActor ID: {actorId},\n\tIdentifiable Type: {type.name}");
            return false;
        }
        
        produceModel.destroyTime = destroyTime;
        produceModel.state = state;
        produceModel.progressTime = progress;
        
        GameState.identifiables[actorId] = model;
        if (GameState.identifiablesByIdent.TryGetValue(type, out var actors))
            actors.Add(model);
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
        networkComponent.previousPosition = position;
        networkComponent.nextPosition = position;
        networkComponent.previousRotation = rotation;
        networkComponent.nextRotation = rotation;
        actor.transform.position = position;
        ActorManager.Actors[actorId.Value] = model;
        
        var cycle = actor.GetComponent<ResourceCycle>();

        if (actorData.JointIndex >= 0 && cycle != null)
        {
            Joint? targetJoint = null;

            if (!string.IsNullOrEmpty(actorData.PlotID))
            {
                if (GameState.landPlots.TryGetValue(actorData.PlotID, out var plotModel) && plotModel.gameObj)
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
                if (cycle._vacuumable)
                    cycle._vacuumable.enabled = false;
            }
            else
            {
                networkComponent.SetResourceState(state, progress, true);
            }
            
            var scale = actorData.Scale > 0f
                ? actorData.Scale
                : (state == ResourceCycle.State.UNRIPE ? 0.33f : 1f);
            actor.transform.localScale = cycle._defaultScale * scale;
        }
        
        return true;
    }
    
    private bool TrySpawnInitialSprinkle(InitialActorsPacket.Sprinkle actorData, out IdentifiableModel? model)
    {
        model = null;
        
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
            SrLogger.LogWarning($"Tried to spawn sprinkle with an invalid type!\n\tActor {actorId.Value}: type_{typeId}");
            return false;
        }
        
        if (!type.prefab)
            return false;
        
        if (ActorIDAlreadyInUse(actorId))
            return false;
        
        model = GameState.CreateActorModel(actorId, type, scene, position, rotation).TryCast<IdentifiableModel>();

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
        networkComponent.previousPosition = position;
        networkComponent.nextPosition = position;
        networkComponent.previousRotation = rotation;
        networkComponent.nextRotation = rotation;
        actor.transform.position = position;
        ActorManager.Actors[actorId.Value] = model;
        
        StartCoroutine(ApplySprinkleMaterial(actor, (SprinkleMaterialType)actorData.MaterialIndex));
        
        return true;
    }
    
    private bool TrySpawnInitialGadget(InitialActorsPacket.ActorBase actorData, out IdentifiableModel? identifiableModel)
    {
        identifiableModel = null;
        
        var actorId = new ActorId(actorData.ActorId);
        if (ActorIDAlreadyInUse(actorId))
            return false;
        
        RemoveExistingGadgetModel(actorId);
        
        switch (actorData)
        {
            case InitialActorsPacket.DroneStation stationData:
                return TrySpawnInitialDroneStation(stationData, out identifiableModel);
            case InitialActorsPacket.LinkedAmmoGadget linkedAmmoData:
                return TrySpawnInitialAmmoGadget(linkedAmmoData, out identifiableModel);
            case InitialActorsPacket.LinkedGadget linkedData:
                return TrySpawnInitialLinkedGadget(linkedData, out identifiableModel);
        }
        
        var sceneId = actorData.Scene;
        var position = actorData.Position;
        var rotation = actorData.Rotation;
        var typeId = actorData.ActorTypeId;

        if (!ActorTypes.TryGetValue(typeId, out var type))
        {
            SrLogger.LogWarning($"Tried to spawn actor with an invalid type!\n\tActor {actorData.ActorId}: type_{typeId}");
            return false;
        }
        
        var scene = NetworkSceneManager.GetSceneGroup(sceneId);
        var model = GameState.CreateGadgetModel(type.Cast<GadgetDefinition>(), actorId, scene, position, false);
        model.eulerRotation = rotation.eulerAngles;
        
        identifiableModel = model.TryCast<IdentifiableModel>();
        
        HandlingPacket = true;
        var gadget = GadgetDirector.InstantiateGadgetFromModel(model);
        HandlingPacket = false;
        
        gadget.transform.SetPositionAndRotation(position, rotation);
        
        if (actorData is InitialActorsPacket.Gadget gadgetData)
            model.waitForChargeupTime = gadgetData.ChargeupTime;

        return true;
    }

    private bool TrySpawnInitialLinkedGadget(InitialActorsPacket.LinkedGadget actorData, out IdentifiableModel? identifiableModel)
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
        var model = GameState.CreateGadgetModel(type.Cast<GadgetDefinition>(), actorId, scene, position, false);
        model.eulerRotation = rotation.eulerAngles;
        
        identifiableModel = model.Cast<IdentifiableModel>();
        
        HandlingPacket = true;
        var gadget = GadgetDirector.InstantiateGadgetFromModel(model);
        HandlingPacket = false;
        
        gadget.transform.SetPositionAndRotation(position, rotation);
        
        model.waitForChargeupTime = actorData.ChargeupTime;

        return true;
    }
    
    private bool TrySpawnInitialDroneStation(InitialActorsPacket.DroneStation actorData, out IdentifiableModel? identifiableModel)
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
        
        NetworkDroneManager.RemoveStationDrone(actorId);

        var scene = NetworkSceneManager.GetSceneGroup(sceneId);
        
        HandlingPacket = true;
        var droneModel = GameState.CreateGadgetModel(type.Cast<GadgetDefinition>(), actorId, scene, position, false)
            ?.TryCast<DroneStationGadgetModel>();
        HandlingPacket = false;

        if (droneModel == null)
        {
            SrLogger.LogWarning($"Drone station gadget {actorId.Value} did not create a DroneStationGadgetModel!");
            return false;
        }

        droneModel.eulerRotation = rotation.eulerAngles;
        droneModel._type = actorData.DroneType;
        droneModel.SetEnergy(SceneContext.Instance.TimeDirector, 0.8333f, actorData.Charge);
        droneModel.IsDroneAtStation._value = actorData.DroneInStation;
        droneModel._taskData = new DroneTaskData()
        {
            SinkType = actorData.Task.Sink,
            SourceType = actorData.Task.Source,
            TargetType = actorData.Task.Target,
            TargetIdentType = ActorTypes[actorData.Task.TargetIdent],
        };
        
        if (droneModel._type == DroneType.RANCH_DRONE)
            droneModel.InitializeForRancher(SceneContext.Instance.DroneDirector);
        else
            droneModel.InitializeForExplorer(SceneContext.Instance.DroneDirector, SceneContext.Instance.TimeDirector,
                SceneContext.Instance.DroneDirector.GetStationAreaResources(droneModel));

        droneModel.Initialized = true;
        
        identifiableModel = droneModel.Cast<IdentifiableModel>();
        
        HandlingPacket = true;
        var gadget = GadgetDirector.InstantiateGadgetFromModel(droneModel.Cast<GadgetModel>());
        HandlingPacket = false;
        gadget.GetComponent<DroneStation>()?.SetModel(droneModel.Cast<GadgetModel>());
        
        gadget.transform.SetPositionAndRotation(position, rotation);

        droneModel.waitForChargeupTime = actorData.ChargeupTime;
        
        StartCoroutine(NetworkDroneManager.SetupInitialStation(droneModel, actorData));

        return true;
    }

    private bool TrySpawnInitialAmmoGadget(InitialActorsPacket.LinkedAmmoGadget actorData, out IdentifiableModel? identifiableModel)
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
        var gadgetModel = GameState.CreateGadgetModel(type.Cast<GadgetDefinition>(), actorId, scene, position, false);
        gadgetModel.eulerRotation = rotation.eulerAngles;
        
        if (gadgetModel.TryCast<WarpDepotModel>(out var depotModel))
            depotModel.ammo = actorData.Ammo.ToGameAmmo()._ammoModel;
        
        identifiableModel = gadgetModel.Cast<IdentifiableModel>();
        
        HandlingPacket = true;
        var gadget = GadgetDirector.InstantiateGadgetFromModel(gadgetModel);
        HandlingPacket = false;
        
        gadget.transform.SetPositionAndRotation(position, rotation);
        
        gadgetModel.waitForChargeupTime = actorData.ChargeupTime;

        return true;
    }
}