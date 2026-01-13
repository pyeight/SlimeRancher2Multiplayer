using SR2MP.Packets.Loading;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.InitialPlots)]
public sealed class PlotsLoadHandler : BaseClientPacketHandler
{
    public PlotsLoadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<LandPlotsPacket>();

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

            if (plot.Data is LandPlotsPacket.GardenData garden)
            {
                if (garden.Crop == 9)
                {
                    model.resourceGrowerDefinition = null;
                    if (model.gameObj)
                    {
                        var gardenPlot = model.gameObj.GetComponentInChildren<LandPlot>();
                        gardenPlot.DestroyAttached();
                    }
                }
                else
                {
                    var actor = actorManager.ActorTypes[garden.Crop];
                    model.resourceGrowerDefinition =
                        GameContext.Instance.AutoSaveDirector._saveReferenceTranslation._resourceGrowerTranslation
                            .RawLookupDictionary._entries.FirstOrDefault(x =>
                                x.value._primaryResourceType == actor)!.value;

                    if (model.gameObj)
                    {
                        var gardenCatcher = model.gameObj.GetComponentInChildren<GardenCatcher>();

                        if (gardenCatcher.CanAccept(actor))
                            gardenCatcher.Plant(actor, true);
                    }
                }
            }
            else if (plot.Data is LandPlotsPacket.SiloData silo) { } // todo
        }
    }
}