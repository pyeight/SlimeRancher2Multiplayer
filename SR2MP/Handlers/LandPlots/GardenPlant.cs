using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.LandPlots;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.LandPlots;

[PacketHandler((byte)PacketType.GardenPlant)]
public sealed class GardenPlantHandler : BasePacketHandler<GardenPlantPacket>
{
    protected override bool Handle(GardenPlantPacket packet, IPEndPoint? _)
    {
        var model = GameState.landPlots[packet.ID];

        if (packet.ActorType == 9)
        {
            model.resourceGrowerDefinition = null;

            if (!model.gameObj) return true;
            var plot = model.gameObj.GetComponentInChildren<LandPlot>();

            handlingPacket = true;
            plot.DestroyAttached();
            handlingPacket = false;
        }
        else
        {
            var actor = actorManager.ActorTypes[packet.ActorType];

            model.resourceGrowerDefinition =
                GameContext.Instance.AutoSaveDirector._saveReferenceTranslation._resourceGrowerTranslation.RawLookupDictionary._entries.FirstOrDefault(x =>
                    x.value._primaryResourceType == actor)!.value;

            if (!model.gameObj) return true;
            var garden = model.gameObj.GetComponentInChildren<GardenCatcher>();

            handlingPacket = true;
            if (garden.CanAccept(actor))
                garden.Plant(actor, true);
            handlingPacket = false;
        }

        return true;
    }
}