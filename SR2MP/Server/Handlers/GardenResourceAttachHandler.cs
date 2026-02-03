using System.Net;
using SR2MP.Packets.Actor;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.ResourceAttach)]
public sealed class GardenResourceAttachHandler : BasePacketHandler<ResourceAttachPacket>
{
    public GardenResourceAttachHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(ResourceAttachPacket packet, IPEndPoint clientEp)
    {
        if (packet.PlotID.Length < 1)
        {
            if (actorManager.Actors.TryGetValue(packet.ActorId.Value, out var model))
            {
                if (!SceneContext.Instance.GameModel.resourceSpawners.TryGetValue(packet.SpawnerID, out var spawnerModel))
                {
                    spawnerModel = packet.Model;
                    SceneContext.Instance.GameModel.resourceSpawners.Add(packet.SpawnerID, spawnerModel);

                    Main.Server.SendToAllExcept(packet, clientEp);

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
                    Main.Server.SendToAllExcept(packet, clientEp);
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

        Main.Server.SendToAllExcept(packet, clientEp);
    }
}