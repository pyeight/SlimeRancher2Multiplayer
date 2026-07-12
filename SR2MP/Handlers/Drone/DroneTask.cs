using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Drone;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.World;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Drone;

[PacketHandler((byte)PacketType.DroneTask)]
internal sealed class DroneTaskHandler : BasePacketHandler<DroneProgramPacket>
{
    protected override bool Handle(DroneProgramPacket packet, IPEndPoint? _)
    {
        var stationModel = GameState.GetIdentifiableModel(new ActorId(packet.ActorId))?.TryCast<DroneStationGadgetModel>();
        if (stationModel == null)
        {
            SrLogger.LogWarning($"DroneStationGadgetModel {packet.ActorId} not found!");
            return true;
        }

        var targetIdentType = (packet.TargetIdent != -1 && ActorManager.ActorTypes.TryGetValue(packet.TargetIdent, out var type)) ? type : null!;

        if (packet.TargetIdent != -1 && targetIdentType == null)
            SrLogger.LogWarning($"Drone task target type {packet.TargetIdent} not found!");

        HandlingPacket = true;

        NetworkDroneManager.ApplyDroneTask(
            stationModel,
            (DroneTaskTargetType)packet.Target,
            targetIdentType,
            (DroneTaskSourceType)packet.Source,
            (DroneTaskSinkType)packet.Sink);

        HandlingPacket = false;
        return true;
    }
}
