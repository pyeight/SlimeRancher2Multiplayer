using SR2MP.Packets.LandPlots;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;
using System.Net;

namespace SR2MP.Shared.Handlers.LandPlots;

[PacketHandler((byte)PacketType.GardenPlant)]
public sealed class GardenPlantHandler : BasePacketHandler<GardenPlantPacket>
{
    public GardenPlantHandler(bool isServerSide)
        : base(isServerSide) { }

    protected override bool Handle(GardenPlantPacket packet, IPEndPoint? _)
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

        return true;
    }
}