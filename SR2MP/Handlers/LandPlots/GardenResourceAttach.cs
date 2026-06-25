using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.LandPlots;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.LandPlots;

[PacketHandler((byte)PacketType.ResourceAttach)]
internal sealed class ResourceAttachHandler : BasePacketHandler<ResourceAttachPacket>
{
    protected override bool Handle(ResourceAttachPacket packet, IPEndPoint? _)
    {
        if (!ActorManager.Actors.TryGetValue(packet.ActorId.Value, out var model))
            return false;

        var actor = model.Cast<ActorModel>();

        if (!actor.TryGetNetworkComponent(out var networkComponent))
            return false;

        var cycle = networkComponent.GetComponent<ResourceCycle>();
        if (cycle == null)
            return false;

        Joint? targetJoint = null;
        SpawnResource? resolvedSpawner = null;
        
        if (!string.IsNullOrEmpty(packet.PlotID)
            && GameState.landPlots.TryGetValue(packet.PlotID, out var plotModel)
            && plotModel.gameObj)
        {
            var plotSpawner = plotModel.gameObj.GetComponentInChildren<SpawnResource>();
            if (plotSpawner != null
                && packet.Joint >= 0
                && packet.Joint < plotSpawner.SpawnJoints.Count)
            {
                targetJoint     = plotSpawner.SpawnJoints[packet.Joint];
                resolvedSpawner = plotSpawner;
            }

            if (plotSpawner?._model != null)
                ApplySpawnerModel(plotSpawner._model, packet.Model);
        }
        
        if (targetJoint == null)
        {
            foreach (var spawner in Object.FindObjectsOfType<SpawnResource>())
            {
                if ((spawner.transform.position - packet.SpawnerID).sqrMagnitude >= 1f)
                    continue;

                if (packet.Joint >= 0 && packet.Joint < spawner.SpawnJoints.Count)
                {
                    targetJoint     = spawner.SpawnJoints[packet.Joint];
                    resolvedSpawner = spawner;
                }

                if (spawner._model != null)
                    ApplySpawnerModel(spawner._model, packet.Model);

                break;
            }
        }

        if (targetJoint == null || resolvedSpawner == null)
            return false;

        HandlingPacket = true;
        cycle.Attach(targetJoint);
        HandlingPacket = false;
        
        resolvedSpawner._spawned.Add(cycle.gameObject);

        var produceModel = model.TryCast<ProduceModel>();
        if (produceModel != null)
            networkComponent.SetResourceState(produceModel._state, produceModel.progressTime, true);

        return true;
    }

    private static void ApplySpawnerModel(SpawnResourceModel dest, SpawnResourceModel src)
    {
        dest.nextSpawnRipens        = src.nextSpawnRipens;
        dest.nextSpawnTime          = src.nextSpawnTime;
        dest.storedWater            = src.storedWater;
        dest.wasPreviouslyPlanted   = src.wasPreviouslyPlanted;
    }
}