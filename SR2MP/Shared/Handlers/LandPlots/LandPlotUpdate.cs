using SR2MP.Packets.LandPlots;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;
using System.Net;

namespace SR2MP.Shared.Handlers.LandPlots;

public abstract class LandPlotUpdateHandler<T> : BasePacketHandler<T> where T : LandPlotUpdatePacket, new()
{
}

[PacketHandler((byte)PacketType.LandPlotUpgrade)]
public sealed class LandPlotUpgradeHandler : LandPlotUpdateHandler<LandPlotUpgradePacket>
{
    protected override bool Handle(LandPlotUpgradePacket packet, IPEndPoint? _)
    {
        var model = SceneContext.Instance.GameModel.landPlots[packet.ID];

        model.upgrades.Add(packet.PlotUpgrade);

        if (model.gameObj)
        {
            var landPlotComponent = model.gameObj.GetComponentInChildren<LandPlot>();
            handlingPacket = true;
            landPlotComponent.AddUpgrade(packet.PlotUpgrade);
            handlingPacket = false;
        }

        return true;
    }
}

[PacketHandler((byte)PacketType.NewLandPlot)]
public sealed class NewLandPlotHandler : BasePacketHandler<NewLandPlotPacket>
{
    protected override bool Handle(NewLandPlotPacket packet, IPEndPoint? _)
    {
        var model = SceneContext.Instance.GameModel.landPlots[packet.ID];

        model.typeId = packet.PlotType;

        if (model.gameObj)
        {
            var location = model.gameObj.GetComponent<LandPlotLocation>();
            var landPlotComponent = model.gameObj.GetComponentInChildren<LandPlot>();

            handlingPacket = true;
            location.Replace(landPlotComponent,
                GameContext.Instance.LookupDirector._plotPrefabDict[packet.PlotType]);
            handlingPacket = false;
        }

        return true;
    }
}