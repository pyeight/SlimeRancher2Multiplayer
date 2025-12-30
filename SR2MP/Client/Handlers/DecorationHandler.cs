using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;
using SR2MP.Packets.Shared;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers
{
    [PacketHandler((byte)PacketType.Decoration)]
    public class DecorationHandler : BaseClientPacketHandler
    {
        public DecorationHandler(Client client, RemotePlayerManager playerManager)
            : base(client, playerManager) { }

        public override void Handle(byte[] data)
        {
            using var reader = new PacketReader(data);
            var packet = reader.ReadPacket<DecorationPacket>();

            // Prevent infinite loop from our own patches
            GlobalVariables.handlingPacket = true;
            try
            {
                var decorizer = SceneContext.Instance.GameModel.GetDecorizerModel();
                if (decorizer == null)
                {
                    MelonLoader.MelonLogger.Warning("DecorizerModel is null, cannot sync decoration.");
                    return;
                }

                if (packet.TypeId == null)
                {
                    MelonLoader.MelonLogger.Warning("DecorationPacket TypeId is null.");
                    return;
                }

                if (packet.IsRemoval)
                {
                    decorizer.Remove(packet.TypeId);
                }
                else
                {
                    decorizer.Add(packet.TypeId);
                }
            }
            catch (System.Exception e)
            {
                MelonLoader.MelonLogger.Error($"Error handling DecorationPacket: {e}");
            }
            finally
            {
                GlobalVariables.handlingPacket = false;
            }
        }
    }
}
