using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Drone;
using SR2MP.Packets.Loading;
using SR2MP.Shared.Utils;

namespace SR2MP.Shared.Managers;

internal sealed partial class NetworkActorManager
{
    private bool TrySpawnNetworkGadget(ActorId actorId, Vector3 position, Quaternion rotation, int typeId, int sceneId, out IdentifiableModel? identModel, double chargeupTime = 0)
    {
        identModel = null;
        
        if (!ActorTypes.TryGetValue(typeId, out var type))
        {
            SrLogger.LogWarning($"Tried to spawn gadget with an invalid type!\n\tActor {actorId}: type_{typeId}");
            return false;
        }
        
        if (!FreeActorIdForGadget(actorId, type))
            return false;

        var scene = NetworkSceneManager.GetSceneGroup(sceneId);

        var model = NetworkGadgetManager.CreateNetworkGadget(type, actorId, scene, position, rotation, out _);
        if (model == null)
            return false;

        if (chargeupTime > 0)
        {
            model.waitForChargeupTime = chargeupTime;
            NetworkGadgetManager.ReSetChargeup(model, chargeupTime);
        }

        var stationModel = model.TryCast<DroneStationGadgetModel>();
        if (stationModel != null)
            StartCoroutine(NetworkDroneManager.EnsureStation(stationModel));

        NetworkGadgetManager.EnsureGadgetLinked(model);
        NetworkGadgetManager.CheckPendingGadgetLink(model);

        identModel = model.TryCast<IdentifiableModel>();
        return true;
    }

    private bool TrySpawnInitialGadget(InitialActorsPacket.ActorBase actorData, out IdentifiableModel? identifiableModel)
    {
        identifiableModel = null;
        
        var actorId = new ActorId(actorData.ActorId);
        var typeId = actorData.ActorTypeId;

        if (!ActorTypes.TryGetValue(typeId, out var type))
        {
            SrLogger.LogWarning($"Tried to spawn gadget with an invalid type!\n\tActor {actorData.ActorId}: type_{typeId}");
            return false;
        }

        if (!FreeActorIdForGadget(actorId, type))
            return false;
        
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
        
        var scene = NetworkSceneManager.GetSceneGroup(sceneId);

        var model = NetworkGadgetManager.CreateNetworkGadget(type, actorId, scene, position, rotation, out _);
        if (model == null)
            return false;

        identifiableModel = model.TryCast<IdentifiableModel>();

        if (actorData is InitialActorsPacket.Gadget gadgetData)
        {
            model.waitForChargeupTime = gadgetData.ChargeupTime;
            NetworkGadgetManager.ReSetChargeup(model, gadgetData.ChargeupTime);
        }

        NetworkGadgetManager.EnsureGadgetLinked(model);
        NetworkGadgetManager.CheckPendingGadgetLink(model);

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
            SrLogger.LogWarning($"Tried to spawn linked gadget with an invalid type!\n\tActor {actorData.ActorId}: type_{typeId}");
            return false;
        }
        
        var scene = NetworkSceneManager.GetSceneGroup(sceneId);

        var model = NetworkGadgetManager.CreateNetworkGadget(type, actorId, scene, position, rotation, out _);
        if (model == null)
            return false;

        identifiableModel = model.Cast<IdentifiableModel>();

        model.waitForChargeupTime = actorData.ChargeupTime;
        NetworkGadgetManager.ReSetChargeup(model, actorData.ChargeupTime);

        NetworkGadgetManager.EnsureGadgetLinked(model);
        NetworkGadgetManager.CheckPendingGadgetLink(model);

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
            SrLogger.LogWarning($"Tried to spawn drone with an invalid type!\n\tActor {actorData.ActorId}: type_{typeId}");
            return false;
        }
        
        NetworkDroneManager.RemoveStationDrone(actorId);

        var scene = NetworkSceneManager.GetSceneGroup(sceneId);
        
        DroneStationGadgetModel? droneModel = null;
        
        NetworkGadgetManager.CreateNetworkGadget(type, actorId, scene, position, rotation, out var gadget, model =>
        {
            droneModel = model.TryCast<DroneStationGadgetModel>();
            if (droneModel == null)
                return;

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
        });

        if (droneModel == null)
        {
            SrLogger.LogWarning($"Drone station gadget {actorId.Value} did not create a DroneStationGadgetModel!");
            return false;
        }

        identifiableModel = droneModel.Cast<IdentifiableModel>();

        if (gadget)
            gadget.GetComponent<DroneStation>()?.SetModel(droneModel.Cast<GadgetModel>());

        droneModel.waitForChargeupTime = actorData.ChargeupTime;
        NetworkGadgetManager.ReSetChargeup(droneModel.TryCast<GadgetModel>(), actorData.ChargeupTime);

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
            SrLogger.LogWarning($"Tried to spawn ammo gadget with an invalid type!\n\tActor {actorData.ActorId}: type_{typeId}");
            return false;
        }
        
        var scene = NetworkSceneManager.GetSceneGroup(sceneId);

        var gadgetModel = NetworkGadgetManager.CreateNetworkGadget(type, actorId, scene, position, rotation, out _, model =>
        {
            if (model.TryCast<WarpDepotModel>(out var depotModel))
                depotModel.ammo = actorData.Ammo.ToGameAmmo()._ammoModel;
            else if (model.TryCast<LinkedCannonModel>(out var cannonModel))
                cannonModel.Ammo = actorData.Ammo.ToGameAmmo()._ammoModel;
        });

        if (gadgetModel == null)
            return false;

        identifiableModel = gadgetModel.Cast<IdentifiableModel>();

        gadgetModel.waitForChargeupTime = actorData.ChargeupTime;
        NetworkGadgetManager.ReSetChargeup(gadgetModel, actorData.ChargeupTime);

        return true;
    }
}
