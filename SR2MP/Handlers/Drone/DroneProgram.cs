using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Drone;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.World;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Drone;

[PacketHandler((byte)PacketType.DroneProgram)]
internal sealed class DroneProgramHandler : BasePacketHandler<DroneProgramPacket>
{
    protected override bool Handle(DroneProgramPacket packet, IPEndPoint? _)
    {
        var stationModel = GameState.GetIdentifiableModel(new ActorId(packet.ActorId))?.TryCast<DroneStationGadgetModel>();
        if (stationModel == null)
        {
            SrLogger.LogWarning($"DroneStationGadgetModel {packet.ActorId} not found!");
            return true;
        }

        HandlingPacket = true;

        var targetIdentType = (packet.TargetIdent != -1 && ActorManager.ActorTypes.TryGetValue(packet.TargetIdent, out var type)) ? type : null!;
        var taskData = new DroneTaskData
        {
            TargetType = (DroneTaskTargetType)packet.Target,
            TargetIdentType = targetIdentType,
            SinkType = (DroneTaskSinkType)packet.Sink,
            SourceType = (DroneTaskSourceType)packet.Source
        };

        var worldTime = SceneContext.Instance.TimeDirector.WorldTime();
        stationModel.SetTaskData(taskData, worldTime);

        if (Main.Server.IsRunning)
            Main.Server.SendToAll(packet);

        HandlingPacket = false;
        return true;
    }
}
