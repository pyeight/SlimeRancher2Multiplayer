using Il2CppMonomiPark.SlimeRancher.Labyrinth;
using Il2CppMonomiPark.SlimeRancher.SceneManagement;
using SR2MP.Packets.Utils;
using SR2MP.Packets.Shared;
using SR2MP.Shared.Managers;
using System.Linq;

namespace SR2MP.Client.Handlers
{
    [PacketHandler((byte)PacketType.PrismaDisruption)]
    public class PrismaDisruptionHandler : BaseClientPacketHandler
    {
        public PrismaDisruptionHandler(Client client, RemotePlayerManager playerManager)
            : base(client, playerManager) { }

        public override void Handle(byte[] data)
        {
            using var reader = new PacketReader(data);
            var packet = reader.ReadPacket<PrismaDisruptionPacket>();

            GlobalVariables.handlingPacket = true;
            try
            {
                var director = SceneContext.Instance.PrismaDirector;
                if (director == null) return;

                // Find the DisruptionArea by iterating the director's dictionary

                var dict = director._disruptionAreas;
                if (dict != null)
                {
                    foreach (var entry in dict)
                    {
                        // entry.Key is DisruptionAreaDefinition
                        // entry.Value is DisruptionArea
                        if (entry.Key.name == packet.AreaId)
                        {
                            director.SetDisruptionLevel(entry.Value, (DisruptionLevel)packet.Level, packet.IsTransition);
                            return; 
                        }
                    }
                }
                else
                {
                     MelonLoader.MelonLogger.Warning("PrismaDirector._disruptionAreas is null or inaccessible.");
                }
            }
            catch (System.Exception e)
            {
                MelonLoader.MelonLogger.Error($"Error handling PrismaDisruptionPacket: {e}");
            }
            finally
            {
                GlobalVariables.handlingPacket = false;
            }
        }
    }
}
