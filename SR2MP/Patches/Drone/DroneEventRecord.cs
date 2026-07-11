using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Drone.Productivity;
using SR2MP.Packets.Drone;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Drone;

[HarmonyPatch(typeof(EventStore), nameof(EventStore.RecordEvent))]
internal static class OnDroneEventRecorded
{
    public static void Postfix(IEvent evt)
    {
        if (HandlingPacket) return;

        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;

        var plortSold = evt.TryCast<PlortSold>();
        if (plortSold != null)
        {
            Main.SendToAllOrServer(new DroneEventPacket
            {
                Kind = DroneEventPacket.EventKind.PlortSold,
                StationId = plortSold.StationId.Value,
                Scene = NetworkSceneManager.GetPersistentID(plortSold.SceneGroup),
                WorldTime = plortSold.WorldTime,
                TypeId = NetworkActorManager.GetPersistentID(plortSold.PlortType),
                Count = plortSold.Count
            });
            return;
        }

        var resourceCollected = evt.TryCast<ResourceCollected>();
        if (resourceCollected != null)
        {
            Main.SendToAllOrServer(new DroneEventPacket
            {
                Kind = DroneEventPacket.EventKind.ResourceCollected,
                StationId = resourceCollected.StationId.Value,
                Scene = NetworkSceneManager.GetPersistentID(resourceCollected.SceneGroup),
                WorldTime = resourceCollected.WorldTime,
                TypeId = NetworkActorManager.GetPersistentID(resourceCollected.ResourceType),
                Count = resourceCollected.Count
            });
        }
    }
}
