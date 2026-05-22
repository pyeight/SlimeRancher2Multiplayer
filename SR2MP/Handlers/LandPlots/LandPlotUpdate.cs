using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.LandPlots;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.LandPlots;

internal abstract class LandPlotUpdateHandler<T> : BasePacketHandler<T> where T : LandPlotUpdatePacket, new()
{
}

[PacketHandler((byte)PacketType.LandPlotUpgrade)]
internal sealed class LandPlotUpgradeHandler : LandPlotUpdateHandler<LandPlotUpgradePacket>
{
    protected override bool Handle(LandPlotUpgradePacket packet, IPEndPoint? _)
    {
        var model = GameState.landPlots[packet.PlotID];

        model.upgrades.Add(packet.ID);

        if (model.gameObj)
        {
            var landPlotComponent = model.gameObj.GetComponentInChildren<LandPlot>();
            HandlingPacket = true;
            landPlotComponent.AddUpgrade(packet.ID);
            HandlingPacket = false;
        }

        return true;
    }
}

[PacketHandler((byte)PacketType.NewLandPlot)]
internal sealed class NewLandPlotHandler : BasePacketHandler<NewLandPlotPacket>
{
    protected override bool Handle(NewLandPlotPacket packet, IPEndPoint? _)
    {
        var model = GameState.landPlots[packet.PlotID];

        model.typeId = packet.ID;

        if (model.gameObj)
        {
            var location = model.gameObj.GetComponent<LandPlotLocation>();
            var landPlotComponent = model.gameObj.GetComponentInChildren<LandPlot>();

            HandlingPacket = true;
            location.Replace(landPlotComponent,
                GameContext.Instance.LookupDirector._plotPrefabDict[packet.ID]);
            HandlingPacket = false;
        }

        return true;
    }
}