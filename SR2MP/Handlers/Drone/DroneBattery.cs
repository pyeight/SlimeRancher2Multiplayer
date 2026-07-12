using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.World;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Drone;

[PacketHandler((byte)PacketType.DroneBattery)]
internal sealed class DroneBatteryHandler : BasePacketHandler<DroneBatteryPacket>
{
    protected override bool Handle(DroneBatteryPacket packet, IPEndPoint? _)
    {
        var stationModel = GameState.GetIdentifiableModel(new ActorId(packet.ActorId))?.TryCast<DroneStationGadgetModel>();
        if (stationModel == null)
        {
            SrLogger.LogWarning($"DroneStationGadgetModel {packet.ActorId} not found!");
            return true;
        }

        HandlingPacket = true;
        stationModel.SetEnergy(SceneContext.Instance.TimeDirector, packet.EnergyDepletedPerHour, packet.Charge);
        HandlingPacket = false;

        return true;
    }
}
