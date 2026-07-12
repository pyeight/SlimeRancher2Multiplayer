using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Drone;
using SR2MP.Packets.Drone;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;

namespace SR2MP.Components.Drone;

internal sealed partial class NetworkDrone
{
    private Vector3 previousPosition;
    private Vector3 nextPosition;
    private Quaternion previousRotation;
    private Quaternion nextRotation;
    private float interpolationStart;
    private float interpolationEnd;

    private Vector3 lastSentPosition;
    private Quaternion lastSentRotation;
    private bool lastSentAtStation;
    private int lastSentAmmoHash;
    private int lastSentAnimState;
    private int lastSentAnimParamsHash;
    private int skippedUpdates;
    private const int ForceSendInterval = 10;

    private void UpdateInterpolation()
    {
        if (interpolationEnd <= interpolationStart)
            return;

        var now = UnityEngine.Time.unscaledTime;

        if (now <= interpolationEnd)
        {
            var t = Mathf.InverseLerp(interpolationStart, interpolationEnd, now);
            transform.position = Vector3.Lerp(previousPosition, nextPosition, t);
            transform.rotation = Quaternion.Lerp(previousRotation, nextRotation, t);
        }
        else
        {
            transform.position = nextPosition;
            transform.rotation = nextRotation;
        }
    }

    private void SendUpdate(bool force)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
            return;

        if (StationId == 0)
            return;

        var currentPosition = transform.position;
        var currentRotation = transform.rotation;
        var atStation = IsAtStation();
        var animatorState = GetCurrentAnimatorState();
        var animatorParams = GetAnimatorParameters();
        var parameterHash = GetParameterHash(animatorParams);

        var ammo = ranchDrone != null ? NetworkDroneManager.CreateDroneAmmo(ranchDrone._model) : null;
        var ammoHash = NetworkDroneManager.GetAmmoHash(ammo);

        var changed = force
                   || currentPosition != lastSentPosition
                   || currentRotation != lastSentRotation
                   || atStation       != lastSentAtStation
                   || ammoHash        != lastSentAmmoHash
                   || animatorState   != lastSentAnimState
                   || parameterHash   != lastSentAnimParamsHash;

        if (!changed && ++skippedUpdates < ForceSendInterval)
            return;

        skippedUpdates = 0;
        lastSentPosition = currentPosition;
        lastSentRotation = currentRotation;
        lastSentAtStation = atStation;
        lastSentAmmoHash = ammoHash;
        lastSentAnimState = animatorState;
        lastSentAnimParamsHash = parameterHash;

        Main.SendToAllOrServer(new DroneUpdatePacket
        {
            StationId = StationId,
            Position = currentPosition,
            Rotation = currentRotation,
            AtStation = atStation,
            AnimatorState = animatorState,
            AnimatorParams = animatorParams,
            Ammo = ammo
        });
    }

    public void ApplyUpdate(DroneUpdatePacket packet)
    {
        if (LocallyOwned)
            return;

        previousPosition = transform.position;
        previousRotation = transform.rotation;
        nextPosition = packet.Position;
        nextRotation = packet.Rotation;
        interpolationStart = UnityEngine.Time.unscaledTime;
        interpolationEnd = interpolationStart + Timers.ActorTimer;

        ApplyAtStation(packet.AtStation);
        ApplyAnimatorParameters(packet.AnimatorParams);
        ApplyAnimatorState(packet.AnimatorState);

        if (packet.Ammo != null && ranchDrone != null)
            NetworkDroneManager.ApplyDroneAmmo(ranchDrone, packet.Ammo);
    }

    private void ApplyAtStation(bool atStation)
    {
        var stationModel = GetStationModel();
        if (stationModel == null || stationModel._isDroneAtStation == atStation)
            return;

        HandlingPacket = true;

        var stationObj = stationModel.GetGameObject();
        var station = stationObj ? stationObj!.GetComponentInChildren<DroneStation>() : null;

        if (station)
            station!.SetIsDroneAtStation(atStation);
        else
            stationModel.IsDroneAtStation._value = atStation;

        HandlingPacket = false;
    }

    private bool IsAtStation()
    {
        var stationModel = GetStationModel();
        return stationModel != null && stationModel._isDroneAtStation;
    }

    private DroneStationGadgetModel? GetStationModel()
    {
        return ranchDrone != null ? ranchDrone._stationModel : explorerDrone?._stationModel;
    }
}
