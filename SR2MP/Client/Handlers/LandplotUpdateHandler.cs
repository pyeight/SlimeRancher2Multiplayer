using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.LandPlotUpdate)]
public sealed class LandplotUpdateHandler : BaseClientPacketHandler
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
            
            // Play Buy Sound
            if (fxManager.worldAudioCueMap.TryGetValue(WorldFXType.BuyPlot, out var cue))
            {
                fxManager.PlayTransientAudio(cue, model.gameObj.transform.position, 0.8f);
            }
            
            handlingPacket = false;

            return;
        }

        model.upgrades.Add(packet.PlotUpgrade);

        if (!model.gameObj) return;
        {
            var landPlotComponent = model.gameObj.GetComponentInChildren<LandPlot>();
            handlingPacket = true;
            landPlotComponent.AddUpgrade(packet.PlotUpgrade);
            
            // Play Upgrade Sound
            if (fxManager.worldAudioCueMap.TryGetValue(WorldFXType.UpgradePlot, out var cue))
            {
                fxManager.PlayTransientAudio(cue, model.gameObj.transform.position, 0.8f);
            }
            
            handlingPacket = false;
        }
    }
}