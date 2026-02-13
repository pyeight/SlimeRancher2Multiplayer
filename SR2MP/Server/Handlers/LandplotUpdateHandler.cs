using System.Net;
using SR2MP.Packets.LandPlots;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

public abstract class LandPlotUpdateHandler<T> : BasePacketHandler<T> where T : LandPlotUpdatePacket, new()
{
    protected LandPlotUpdateHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(T packet, IPEndPoint clientEp) => Main.Server.SendToAllExcept(packet, clientEp);
}

[PacketHandler((byte)PacketType.LandPlotUpgrade)]
public sealed class LandPlotUpgradeHandler : LandPlotUpdateHandler<LandPlotUpgradePacket>
{
    public LandPlotUpgradeHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(LandPlotUpgradePacket packet, IPEndPoint clientEp)
    {
        var model = SceneContext.Instance.GameModel.landPlots[packet.ID];
        model.upgrades.Add(packet.PlotUpgrade);

        if (model.gameObj)
            model.gameObj.GetComponentInChildren<LandPlot>().AddUpgrade(packet.PlotUpgrade);
    }
}

[PacketHandler((byte)PacketType.NewLandPlot)]
public sealed class NewLandPlotHandler : BasePacketHandler<NewLandPlotPacket>
{
    public NewLandPlotHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(NewLandPlotPacket packet, IPEndPoint clientEp)
    {
        var model = SceneContext.Instance.GameModel.landPlots[packet.ID];
        model.typeId = packet.PlotType;

        if (!model.gameObj)
            return;

        var location = model.gameObj.GetComponent<LandPlotLocation>();
        var landPlotComponent = model.gameObj.GetComponentInChildren<LandPlot>();

        location.Replace(landPlotComponent,
            GameContext.Instance.LookupDirector._plotPrefabDict[packet.PlotType]);
    }
}