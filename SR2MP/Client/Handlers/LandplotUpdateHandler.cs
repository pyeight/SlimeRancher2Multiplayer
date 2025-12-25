using Il2Cpp;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.LandPlotUpdate)]
public class LandplotUpdateHandler : BaseClientPacketHandler
{
    public LandplotUpdateHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<LandPlotUpdatePacket>();

        var model = SceneContext.Instance.GameModel.landPlots[packet.ID];

        if (!packet.IsUpgrade)
        {
            model.typeId = packet.PlotType;

            if (!model.gameObj) return;
            
            var location = model.gameObj.GetComponent<LandPlotLocation>();
            var landPlotComponent = model.gameObj.GetComponentInChildren<LandPlot>();

            handlingPacket = true;
            location.Replace(landPlotComponent,
                GameContext.Instance.LookupDirector._plotPrefabDict[packet.PlotType]);
            handlingPacket = false;
            
            return;
        }

        model.upgrades.Add(packet.PlotUpgrade);

        if (!model.gameObj) return;
        {
            var landPlotComponent = model.gameObj.GetComponentInChildren<LandPlot>();
            handlingPacket = true;
            landPlotComponent.AddUpgrade(packet.PlotUpgrade);
            handlingPacket = false;
        }
    }
}