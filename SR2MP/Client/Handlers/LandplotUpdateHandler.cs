using SR2MP.Packets.Landplot;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.LandPlotUpdate)]
public sealed class LandPlotUpdateHandler : BaseClientPacketHandler<LandPlotUpdatePacket>
{
    public LandPlotUpdateHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(LandPlotUpdatePacket packet)
    {
        var model = SceneContext.Instance.GameModel.landPlots[packet.ID];

        if (!packet.IsUpgrade)
        {
            model.typeId = packet.PlotType;

            if (!model.gameObj) return;

            var location = model.gameObj.GetComponent<LandPlotLocation>();
            var landPlotComponent2 = model.gameObj.GetComponentInChildren<LandPlot>();

            handlingPacket = true;
            location.Replace(landPlotComponent2,
                GameContext.Instance.LookupDirector._plotPrefabDict[packet.PlotType]);
            handlingPacket = false;

            return;
        }

        model.upgrades.Add(packet.PlotUpgrade);

        if (!model.gameObj) return;

        var landPlotComponent = model.gameObj.GetComponentInChildren<LandPlot>();
        handlingPacket = true;
        landPlotComponent.AddUpgrade(packet.PlotUpgrade);
        handlingPacket = false;
    }
}