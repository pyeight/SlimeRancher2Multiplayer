using System.Net;
using Il2Cpp;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.LandPlotUpdate)]
public class LandplotUpdateHandler : BasePacketHandler
{
    public LandplotUpdateHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, IPEndPoint senderEndPoint)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<LandPlotUpdatePacket>();

        var model = SceneContext.Instance.GameModel.landPlots[packet.ID];

        Main.Server.SendToAllExcept(packet, senderEndPoint);
        
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