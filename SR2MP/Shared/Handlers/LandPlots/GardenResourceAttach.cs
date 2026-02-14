using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;
using System.Net;

namespace SR2MP.Shared.Handlers.LandPlots;

[PacketHandler((byte)PacketType.ResourceAttach)]
public sealed class GardenResourceAttachHandler : BasePacketHandler<ResourceAttachPacket>
{
    protected override bool Handle(ResourceAttachPacket packet, IPEndPoint? _)
    {
        if (packet.PlotID.Length < 1)
        {
            if (actorManager.Actors.TryGetValue(packet.ActorId.Value, out var model))
            {
                if (!SceneContext.Instance.GameModel.resourceSpawners.TryGetValue(packet.SpawnerID, out var spawnerModel))
                {
                    spawnerModel = packet.Model;
                    SceneContext.Instance.GameModel.resourceSpawners.Add(packet.SpawnerID, spawnerModel);

                    return true;
                }

                spawnerModel.nextSpawnRipens = packet.Model.nextSpawnRipens;
                spawnerModel.nextSpawnTime = packet.Model.nextSpawnTime;
                spawnerModel.storedWater = packet.Model.storedWater;
                spawnerModel.wasPreviouslyPlanted = packet.Model.wasPreviouslyPlanted;

                var spawner = spawnerModel.part.Cast<SpawnResource>();
                if (spawner)
                {
                    var joint = spawner.SpawnJoints[packet.Joint];
                    if (joint!.connectedBody)
                    {
                        SceneContext.Instance.GameModel.identifiables.Remove(packet.ActorId);
                        SceneContext.Instance.GameModel.identifiablesByIdent[model.ident].Remove(model);
                        SceneContext.Instance.GameModel.DestroyIdentifiableModel(model);

                        var obj = model.GetGameObject();
                        if (obj)
                            Destroyer.DestroyActor(model.GetGameObject(), "SR2MP.GardenResourceAttachHandler#1");
                        return false;
                    }
                    model.GetGameObject()?.GetComponent<ResourceCycle>().Attach(joint);
                }
            }
        }
        else if (actorManager.Actors.TryGetValue(packet.ActorId.Value, out var model))
        {
            if (!SceneContext.Instance.GameModel.landPlots.TryGetValue(packet.PlotID, out var plotModel))
            {
                return true;
            }

            var spawner = plotModel.gameObj.GetComponentInChildren<SpawnResource>();
            if (spawner)
            {
                var spawnerModel = spawner._model;
                spawnerModel.nextSpawnRipens = packet.Model.nextSpawnRipens;
                spawnerModel.nextSpawnTime = packet.Model.nextSpawnTime;
                spawnerModel.storedWater = packet.Model.storedWater;
                spawnerModel.wasPreviouslyPlanted = packet.Model.wasPreviouslyPlanted;

                var joint = spawner.SpawnJoints[packet.Joint];
                if (joint!.connectedBody)
                {
                    SceneContext.Instance.GameModel.identifiables.Remove(packet.ActorId);
                    SceneContext.Instance.GameModel.identifiablesByIdent[model.ident].Remove(model);
                    SceneContext.Instance.GameModel.DestroyIdentifiableModel(model);

                    var obj = model.GetGameObject();
                    if (obj)
                        Destroyer.DestroyActor(model.GetGameObject(), "SR2MP.GardenResourceAttachHandler#2");
                    return false;
                }
                var gameObject = model.GetGameObject();
                if (gameObject)
                {
                    var cycle = gameObject.GetComponent<ResourceCycle>();
                    if (cycle)
                        cycle.Attach(joint);
                }
            }
        }

        return true;
    }
}