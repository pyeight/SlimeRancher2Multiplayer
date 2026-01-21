using SR2MP.Packets.Actor;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.ResourceAttach)]
public sealed class GardenResourceAttachHandler : BaseClientPacketHandler<ResourceAttachPacket>
{
    public GardenResourceAttachHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(ResourceAttachPacket packet)
    {
        if (packet.PlotID.Length < 1)
        {
            if (actorManager.Actors.TryGetValue(packet.ActorId.Value, out var model))
            {
                if (!SceneContext.Instance.GameModel.resourceSpawners.TryGetValue(packet.SpawnerID, out var spawnerModel))
                {
                    spawnerModel = packet.Model;
                    SceneContext.Instance.GameModel.resourceSpawners.Add(packet.SpawnerID, spawnerModel);

                    return;
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
                        return;
                    }
                    model.GetGameObject()?.GetComponent<ResourceCycle>().Attach(joint);
                }
            }
        }
        else
        {
            if (actorManager.Actors.TryGetValue(packet.ActorId.Value, out var model))
            {
                if (!SceneContext.Instance.GameModel.landPlots.TryGetValue(packet.PlotID, out var plotModel))
                {
                    return;
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
                        return;
                    }
                    model.GetGameObject()?.GetComponent<ResourceCycle>().Attach(joint);
                }
            }
        }
    }
}