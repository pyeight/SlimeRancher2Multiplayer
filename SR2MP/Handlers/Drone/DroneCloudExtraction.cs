using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Drone;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Drone;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Drone;

[PacketHandler((byte)PacketType.DroneCloudExtraction)]
internal sealed class DroneCloudExtractionHandler : BasePacketHandler<DroneCloudExtractionPacket>
{
    protected override bool Handle(DroneCloudExtractionPacket packet, IPEndPoint? _)
    {
        var stationModel = GameState.GetIdentifiableModel(new ActorId(packet.StationId))?.TryCast<DroneStationGadgetModel>();
        if (stationModel == null)
        {
            SrLogger.LogWarning($"DroneStationGadgetModel {packet.StationId} not found!");
            return true;
        }

        var stationObj = stationModel.GetGameObject();
        var station = stationObj ? stationObj!.GetComponentInChildren<DroneStation>() : null;
        if (!station)
            return true;

        var type = packet.TypeId != -1 && ActorManager.ActorTypes.TryGetValue(packet.TypeId, out var identType)
            ? identType
            : null;

        HandlingPacket = true;
        station!.AssignCloudExtractionMode(packet.Extracting, type, packet.Count);
        HandlingPacket = false;

        return true;
    }
}
