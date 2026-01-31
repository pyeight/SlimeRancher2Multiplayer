using LiteNetLib;
using SR2MP.Packets.Landplot;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.LandPlotUpdate)]
public sealed class LandPlotUpdateHandler : BasePacketHandler<LandPlotUpdatePacket>
{
    public LandPlotUpdateHandler(ClientManager clientManager)
        : base(clientManager) { }

    public override void Handle(LandPlotUpdatePacket packet, NetPeer clientPeer)
    {
        var model = SceneContext.Instance.GameModel.landPlots[packet.ID];

        Main.Server.SendToAllExcept(packet, clientPeer);

        if (!packet.IsUpgrade)
        {
            model.typeId = packet.PlotType;

            if (!model.gameObj) return;

            var location = model.gameObj.GetComponent<LandPlotLocation>();
            var landPlotComponent = model.gameObj.GetComponentInChildren<LandPlot>();

            location.Replace(landPlotComponent,
                GameContext.Instance.LookupDirector._plotPrefabDict[packet.PlotType]);
            return;
        }

        model.upgrades.Add(packet.PlotUpgrade);

        if (!model.gameObj) return;
        {
            var landPlotComponent = model.gameObj.GetComponentInChildren<LandPlot>();
            landPlotComponent.AddUpgrade(packet.PlotUpgrade);
        }
    }
}