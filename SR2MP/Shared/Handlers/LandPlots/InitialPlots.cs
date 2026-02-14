using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;
using System.Net;

namespace SR2MP.Shared.Handlers.LandPlots;

[PacketHandler((byte)PacketType.InitialPlots, HandlerType.Client)]
public sealed class InitialPlotsHandler : BasePacketHandler<InitialLandPlotsPacket>
{
    public InitialPlotsHandler(bool isServerSide)
        : base(isServerSide) { }

    protected override bool Handle(InitialLandPlotsPacket packet, IPEndPoint? _)
    {
        foreach (var plot in packet.Plots)
        {
            var model = SceneContext.Instance.GameModel.landPlots[plot.ID];

            if (model.gameObj)
            {
                handlingPacket = true;
                var location = model.gameObj.GetComponent<LandPlotLocation>();
                var landPlotComponent = model.gameObj.GetComponentInChildren<LandPlot>();
                location.Replace(landPlotComponent, GameContext.Instance.LookupDirector._plotPrefabDict[plot.Type]);

                var landPlotComponent2 = model.gameObj.GetComponentInChildren<LandPlot>();
                landPlotComponent2.ApplyUpgrades(plot.Upgrades.Cast<CppCollections.IEnumerable<LandPlot.Upgrade>>(), false);
                handlingPacket = false;
            }

            model.typeId = plot.Type;
            model.upgrades = plot.Upgrades;

            switch (plot.Data)
            {
                case InitialLandPlotsPacket.GardenData { Crop: 9 }:
                {
                    model.resourceGrowerDefinition = null;
                    if (!model.gameObj)
                        continue;
                    var gardenPlot = model.gameObj.GetComponentInChildren<LandPlot>();
                    gardenPlot.DestroyAttached();
                    break;
                }
                case InitialLandPlotsPacket.GardenData garden:
                {
                    var actor = actorManager.ActorTypes[garden.Crop];
                    model.resourceGrowerDefinition =
                        GameContext.Instance.AutoSaveDirector._saveReferenceTranslation._resourceGrowerTranslation
                           .RawLookupDictionary._entries.FirstOrDefault(x =>
                                x.value._primaryResourceType == actor)!.value;

                    if (!model.gameObj)
                        continue;
                    var gardenCatcher = model.gameObj.GetComponentInChildren<GardenCatcher>();

                    if (gardenCatcher.CanAccept(actor))
                        gardenCatcher.Plant(actor, true);
                    break;
                }
                case InitialLandPlotsPacket.SiloData silo: break; // todo
            }
        }

        return false;
    }
}