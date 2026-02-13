using SR2MP.Client.Handlers.Internal;
using SR2MP.Packets.LandPlots;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers.Landplot;

public abstract class LandPlotUpdateHandler<T> : BaseClientPacketHandler<T> where T : LandPlotUpdatePacket, new()
{
    protected LandPlotUpdateHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }
}

[PacketHandler((byte)PacketType.LandPlotUpgrade)]
public sealed class LandPlotUpgradeHandler : LandPlotUpdateHandler<LandPlotUpgradePacket>
{
    public LandPlotUpgradeHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(LandPlotUpgradePacket packet)
    {
        var model = SceneContext.Instance.GameModel.landPlots[packet.ID];

        model.upgrades.Add(packet.PlotUpgrade);

        if (!model.gameObj)
            return;

        var landPlotComponent = model.gameObj.GetComponentInChildren<LandPlot>();
        handlingPacket = true;
        landPlotComponent.AddUpgrade(packet.PlotUpgrade);
        handlingPacket = false;
    }
}

[PacketHandler((byte)PacketType.NewLandPlot)]
public sealed class NewLandPlotHandler : BaseClientPacketHandler<NewLandPlotPacket>
{
    public NewLandPlotHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(NewLandPlotPacket packet)
    {
        var model = SceneContext.Instance.GameModel.landPlots[packet.ID];

        model.typeId = packet.PlotType;

        if (!model.gameObj)
            return;

        var location = model.gameObj.GetComponent<LandPlotLocation>();
        var landPlotComponent2 = model.gameObj.GetComponentInChildren<LandPlot>();

        handlingPacket = true;
        location.Replace(landPlotComponent2,
            GameContext.Instance.LookupDirector._plotPrefabDict[packet.PlotType]);
        handlingPacket = false;
    }
}