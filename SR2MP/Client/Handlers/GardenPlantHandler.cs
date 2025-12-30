using SR2MP.Packets.Utils;
using SR2MP.Packets.Shared;
using SR2MP.Shared.Managers;
using UnityEngine;

namespace SR2MP.Client.Handlers
{
    [PacketHandler((byte)PacketType.GardenPlant)]
    public class GardenPlantHandler : BaseClientPacketHandler
    {
        public GardenPlantHandler(Client client, RemotePlayerManager playerManager)
            : base(client, playerManager) { }

        public override void Handle(byte[] data)
        {
            using var reader = new PacketReader(data);
            var packet = reader.ReadPacket<GardenPlantPacket>();

            GlobalVariables.handlingPacket = true;
            try
            {
                // Find the garden by plot ID
                var gameModel = SceneContext.Instance.GameModel;
                if (gameModel == null) return;

                var plotModel = gameModel.GetLandPlotModel(packet.PlotId);
                if (plotModel == null) return;

                // Find the actual garden GameObject
                var plotLocation = GameObject.Find(packet.PlotId)?.GetComponent<LandPlotLocation>();
                if (plotLocation == null) return;

                var garden = plotLocation.GetComponentInChildren<GardenCatcher>();
                if (garden == null) return;

                // Get the plant type
                var plantType = GlobalVariables.actorManager.GetIdentifiableType(packet.PlantTypeId);
                if (plantType == null) return;

                // Plant it
                garden.Plant(plantType, false);
            }
            catch (System.Exception e)
            {
                MelonLoader.MelonLogger.Error($"Error handling GardenPlantPacket: {e}");
            }
            finally
            {
                GlobalVariables.handlingPacket = false;
            }
        }
    }
}
