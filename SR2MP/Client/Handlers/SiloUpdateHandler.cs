using SR2MP.Packets.Utils;
using SR2MP.Packets.Shared;
using SR2MP.Shared.Managers;
using UnityEngine;

namespace SR2MP.Client.Handlers
{
    [PacketHandler((byte)PacketType.SiloUpdate)]
    public class SiloUpdateHandler : BaseClientPacketHandler
    {
        public SiloUpdateHandler(Client client, RemotePlayerManager playerManager)
            : base(client, playerManager) { }

        public override void Handle(byte[] data)
        {
            using var reader = new PacketReader(data);
            var packet = reader.ReadPacket<SiloUpdatePacket>();

            GlobalVariables.handlingPacket = true;
            try
            {
                // Find the silo by plot ID
                var plotLocation = GameObject.Find(packet.PlotId)?.GetComponent<LandPlotLocation>();
                if (plotLocation == null) return;

                var silo = plotLocation.GetComponentInChildren<SiloStorage>();
                if (silo == null) return;

                // Get the item type
                var itemType = packet.ItemTypeId != 0 ? GlobalVariables.actorManager.GetIdentifiableType(packet.ItemTypeId) : null;

                // For now, just log the update - full implementation would require
                // more complex ammo manipulation that we don't have access to
                // The silo state will be synced through the normal add/remove operations
                MelonLoader.MelonLogger.Msg($"Silo update received: Plot={packet.PlotId}, Slot={packet.SlotIndex}, Type={itemType?.name}, Count={packet.Count}");
            }
            catch (System.Exception e)
            {
                MelonLoader.MelonLogger.Error($"Error handling SiloUpdatePacket: {e}");
            }
            finally
            {
                GlobalVariables.handlingPacket = false;
            }
        }
    }
}
