using System.Net;
using Il2CppMonomiPark.SlimeRancher.Drone;
using SR2MP.Components.Drone;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Drone;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Drone;

[PacketHandler((byte)PacketType.DroneAnimation)]
internal sealed class DroneAnimationHandler : BasePacketHandler<DroneAnimationPacket>
{
    protected override bool Handle(DroneAnimationPacket packet, IPEndPoint? _)
    {
        if (!NetworkDrone.Drones.TryGetValue(packet.StationId, out var drone) || !drone || drone.LocallyOwned)
            return true;

        var manager = drone.GetComponentInChildren<ExplorerDroneMovementManager>(true);
        if (!manager)
            return true;

        HandlingPacket = true;

        try
        {
            switch (packet.Animation)
            {
                case DroneAnimationPacket.DroneAnimation.Scan:
                    manager!.StartScan();
                    break;
                case DroneAnimationPacket.DroneAnimation.Gather:
                    manager!.StartGather();
                    break;
                case DroneAnimationPacket.DroneAnimation.Acquisition:
                    manager!.StartAcquisitionEvent();
                    break;
                case DroneAnimationPacket.DroneAnimation.StopAnimation:
                    manager!.StopCurrentAnimationState();
                    break;
            }
        }
        finally
        {
            HandlingPacket = false;
        }

        return true;
    }
}
