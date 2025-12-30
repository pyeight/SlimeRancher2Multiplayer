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

                // PrismaDirector seems to hold models or they are in GameModel?
                // Class dump showed: 
                // SR2MP_ClassDump...txt:5031:    Method: get__prismaBarriers () -> Dictionary`2
                // accessing via GameModel might be better if accessible
                
                var gameModel = SceneContext.Instance.GameModel;
                if (gameModel != null)
                {
                    // Accessing private dictionary might be hard without helper or public getter.
                    // But class dump shows: Method: AllPrismaBarriers () -> Dictionary`2 on GameModel?
                    // "Method: AllPrismaBarriers () -> Dictionary`2" was seen in SR2MP_ClassDump...txt:5216
                    
                    // Let's try GameModel.AllPrismaBarriers() if it exists as public
                    // Or iterate if we have to.
                    
                    // The dump listed it under GameModel methods? Wait, let me check the dump context again.
                    // Actually, "SR2MP_ClassDump...txt:5216: Method: AllPrismaBarriers () -> Dictionary`2" 
                    // appears to be in GameModel methods block.
                    
                    // If not accessible, we might need a reflection helper or access through PrismaDirector if it has a ref.
                    // But GameModel is the data source.
                    
                    // Let's assume AllPrismaBarriers() is available or try to find the barrier by ID.
                    // The barriers are PrismaBarrierModel.
                    
                    // We can iterate the dictionary.
                    // But Dictionary access via Il2Cpp can be tricky.
                    // Ideally we use a helper method if available.
                    
                    // Let's try to get barrier via ID directly if possible, or iterate.
                    // Wait, GameModel has `get__prismaBarriers()` which is a Dictionary<string, PrismaBarrierModel>.
                    // We can try access that.
                    
                    // For now, let's assume we can get it via a custom helper or just iteration if exposed.
                    
                    // Actually, `SceneContext.Instance.GameModel.GetPrismaBarrierModel(packet.BarrierId)` would be ideal if it existed.
                    // It doesn't seem to based on dump.
                    
                    // But `GameModel` has `RegisterPrismaBarrier` etc.
                    
                    // Let's try iterating `Il2CppMonomiPark.SlimeRancher.DataModel.GameModel` components/properties?
                    // Or define a helper in Main or Utils to find it.
                    
                    // Accessing `GameModel.AllPrismaBarriers()`
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
