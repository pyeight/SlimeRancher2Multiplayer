using System.Collections;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Drone;
using SR2MP.Components.Drone;
using SR2MP.Packets.Loading;

namespace SR2MP.Shared.Managers;

internal static partial class NetworkDroneManager
{
    internal static IEnumerator EnsureStation(DroneStationGadgetModel stationModel)
    {
        yield return new WaitFrames(3);

        SpawnStationDrone(stationModel);
    }

    private static void SpawnStationDrone(DroneStationGadgetModel? stationModel)
    {
        if (stationModel == null)
            return;

        var stationObj = stationModel.GetGameObject();
        if (!stationObj)
            return;

        var station = stationObj!.GetComponentInChildren<DroneStation>();
        if (!station)
            return;

        // Drone already exists
        if (NetworkDrone.Drones.TryGetValue(stationModel.actorId.Value, out var existingDrone) && existingDrone)
            return;

        if (station!.RanchDrone != null || station.ExplorerDrone != null)
            return;

        HandlingPacket = true;

        try
        {
            if (stationModel._type == DroneType.RANCH_DRONE)
            {
                if (!GameState.droneModel.TryGetRanchDroneByStationId(stationModel.actorId, out _))
                    station.RegisterStationAndSpawnRanchDrone();
            }
            else
            {
                if (!GameState.droneModel.TryGetExplorerDroneByStationId(stationModel.actorId, out _))
                    station.RegisterStationAndSpawnExplorerDrone();
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"Failed to spawn drone for station {stationModel.actorId.Value}: {ex.Message}");
        }

        HandlingPacket = false;
    }

    internal static void ApplyDroneTask(
        DroneStationGadgetModel stationModel,
        DroneTaskTargetType targetType,
        IdentifiableType? targetIdentType,
        DroneTaskSourceType sourceType,
        DroneTaskSinkType sinkType)
    {
        if (AssignTaskToStation(stationModel, targetType, targetIdentType, sourceType, sinkType))
            return;

        var taskData = new DroneTaskData
        {
            TargetType = targetType,
            TargetIdentType = targetIdentType,
            SinkType = sinkType,
            SourceType = sourceType
        };

        stationModel.SetTaskData(taskData, SceneContext.Instance.TimeDirector.WorldTime());

        UpdateStationIcon(stationModel, targetType, targetIdentType);
        StartCoroutine(RetryAssignTaskToStation(stationModel, targetType, targetIdentType, sourceType, sinkType));
    }

    private static void UpdateStationIcon(
        DroneStationGadgetModel stationModel,
        DroneTaskTargetType targetType,
        IdentifiableType? targetIdentType)
    {
        try
        {
            var stationObj = stationModel.GetGameObject();
            if (!stationObj)
                return;

            var display = stationObj!.GetComponentInChildren<DroneStationTargetDisplay>(true);
            if (display)
                display!.SetIcon(targetType, targetIdentType);
        }
        catch (Exception ex)
        {
            SrLogger.LogDebug($"Failed to update drone station icon: {ex.Message}");
        }
    }

    private static IEnumerator RetryAssignTaskToStation(
        DroneStationGadgetModel stationModel,
        DroneTaskTargetType targetType,
        IdentifiableType? targetIdentType,
        DroneTaskSourceType sourceType,
        DroneTaskSinkType sinkType)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            yield return new WaitFrames(5);

            if (stationModel == null)
                yield break;

            // A newer assignment happened
            var current = stationModel._taskData;
            if (current.TargetType != targetType || current.SourceType != sourceType || current.SinkType != sinkType)
                yield break;

            var assigned = false;
            HandlingPacket = true;

            try
            {
                assigned = AssignTaskToStation(stationModel, targetType, targetIdentType, sourceType, sinkType);
            }
            catch { /* station still initializing */ }
            finally
            {
                HandlingPacket = false;
            }

            if (assigned)
                yield break;
        }
    }

    private static bool AssignTaskToStation(
        DroneStationGadgetModel stationModel,
        DroneTaskTargetType targetType,
        IdentifiableType? targetIdentType,
        DroneTaskSourceType sourceType,
        DroneTaskSinkType sinkType)
    {
        var gameObj = stationModel.GetGameObject();
        if (!gameObj)
            return false;

        var station = gameObj!.GetComponentInChildren<DroneStation>();
        if (!station)
            return false;

        if (targetType == DroneTaskTargetType.NONE)
        {
            station!.AssignTaskNone();
            return true;
        }

        var ranchDrone = station!.RanchDrone;
        if (ranchDrone != null)
        {
            var target = ranchDrone.GatherTargetTask(targetType, targetIdentType);
            var source = ranchDrone.GatherSourceTask(sourceType);
            var sink = ranchDrone.GatherSinkTask(sinkType);

            station.AssignRanchTask(target, source, sink, false);
            return true;
        }

        var explorerDrone = station.ExplorerDrone;
        if (explorerDrone != null)
        {
            var target = explorerDrone.GatherTargetTask(targetType, targetIdentType);

            station.AssignExplorerTask(target, false);
            return true;
        }

        return false;
    }

    internal static IEnumerator SetupInitialStation(DroneStationGadgetModel stationModel, InitialActorsPacket.DroneStation actorData)
    {
        if (!string.IsNullOrEmpty(actorData.DroneOwnerId))
            PendingOwners[actorData.ActorId] = actorData.DroneOwnerId;

        yield return new WaitFrames(3);

        SpawnStationDrone(stationModel);

        yield return new WaitFrames(2);

        if (stationModel == null)
            yield break;

        HandlingPacket = true;

        try
        {
            if (stationModel._type == DroneType.RANCH_DRONE &&
                actorData.Ammo != null && actorData.Ammo.AmmoSlots.Count > 0)
            {
                var stationObj = stationModel.GetGameObject();
                var station = stationObj ? stationObj!.GetComponentInChildren<DroneStation>() : null;
                var ranchDrone = station ? station!.RanchDrone : null;

                if (ranchDrone != null)
                    ApplyDroneAmmo(ranchDrone, actorData.Ammo);
            }

            var targetIdent = actorData.Task.TargetIdent != -1 &&
                              ActorManager.ActorTypes.TryGetValue(actorData.Task.TargetIdent, out var type)
                ? type
                : null;

            ApplyDroneTask(stationModel, actorData.Task.Target, targetIdent, actorData.Task.Source, actorData.Task.Sink);
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"Failed to set up initial drone station {stationModel.actorId.Value}: {ex.Message}");
        }

        HandlingPacket = false;
    }
}
