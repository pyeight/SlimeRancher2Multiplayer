using SR2MP.Packets.Landplot;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.GardenPlant)]
public sealed class GardenPlantHandler : BaseClientPacketHandler<GardenPlantPacket>
{
    public GardenPlantHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(GardenPlantPacket packet)
    {
        var model = SceneContext.Instance.GameModel.landPlots[packet.ID];

        if (packet.ActorType == 9)
        {
            model.resourceGrowerDefinition = null;

            if (model.gameObj)
            {
                var plot = model.gameObj.GetComponentInChildren<LandPlot>();

                handlingPacket = true;
                plot.DestroyAttached();
                handlingPacket = false;
            }
        }
        else
        {
            var actor = actorManager.ActorTypes[packet.ActorType];

            model.resourceGrowerDefinition =
                GameContext.Instance.AutoSaveDirector._saveReferenceTranslation._resourceGrowerTranslation.RawLookupDictionary._entries.FirstOrDefault(x =>
                    x.value._primaryResourceType == actor)!.value;

            if (model.gameObj)
            {
                var garden = model.gameObj.GetComponentInChildren<GardenCatcher>();

                handlingPacket = true;
                if (garden.CanAccept(actor))
                    garden.Plant(actor, true);
                handlingPacket = false;
            }
        }
    }
}