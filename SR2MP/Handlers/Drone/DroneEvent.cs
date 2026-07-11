using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Drone.Productivity;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Drone;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Drone;

[PacketHandler((byte)PacketType.DroneEvent)]
internal sealed class DroneEventHandler : BasePacketHandler<DroneEventPacket>
{
    protected override bool Handle(DroneEventPacket packet, IPEndPoint? _)
    {
        if (!ActorManager.ActorTypes.TryGetValue(packet.TypeId, out var type) || type == null)
            return true;

        var eventStore = GameState.droneModel.EventStore;
        if (eventStore == null)
            return true;

        var stationId = new ActorId(packet.StationId);
        var scene = NetworkSceneManager.GetSceneGroup(packet.Scene);

        HandlingPacket = true;

        try
        {
            var evt = packet.Kind switch
            {
                DroneEventPacket.EventKind.PlortSold =>
                    new PlortSold(stationId, scene, packet.WorldTime, type, packet.Count).Cast<IEvent>(),
                _ =>
                    new ResourceCollected(stationId, scene, packet.WorldTime, type, packet.Count).Cast<IEvent>()
            };

            eventStore.RecordEvent(evt);
        }
        catch { /* ignored */ }

        HandlingPacket = false;
        return true;
    }
}
