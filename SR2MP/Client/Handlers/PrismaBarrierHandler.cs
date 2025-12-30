using Il2CppMonomiPark.SlimeRancher.SceneManagement;
using SR2MP.Packets.Utils;
using SR2MP.Packets.Shared;
using SR2MP.Shared.Managers;
using System.Linq;

namespace SR2MP.Client.Handlers
{
    [PacketHandler((byte)PacketType.PrismaBarrier)]
    public class PrismaBarrierHandler : BaseClientPacketHandler
    {
        public PrismaBarrierHandler(Client client, RemotePlayerManager playerManager)
            : base(client, playerManager) { }

        public override void Handle(byte[] data)
        {
            using var reader = new PacketReader(data);
            var packet = reader.ReadPacket<PrismaBarrierPacket>();

            // Avoid infinite loop if we patch the setter
            GlobalVariables.handlingPacket = true;
            try
            {
                var prismaDirector = SceneContext.Instance.PrismaDirector;
                if (prismaDirector == null)
                {
                    // Maybe scene not loaded yet?
                    return;
                }

                // Access Prisma barriers through GameModel
                var gameModel = SceneContext.Instance.GameModel;
                if (gameModel != null)
                {
                    var barriers = gameModel.AllPrismaBarriers();
                    if (barriers != null && barriers.ContainsKey(packet.BarrierId))
                    {
                         var barrier = barriers[packet.BarrierId];
                         if (barrier != null)
                         {
                             barrier.ActivationTime = packet.ActivationTime;
                         }
                    }
                }
            }
            catch (System.Exception e)
            {
                MelonLoader.MelonLogger.Error($"Error handling PrismaBarrierPacket: {e}");
            }
            finally
            {
                GlobalVariables.handlingPacket = false;
            }
        }
    }
}
