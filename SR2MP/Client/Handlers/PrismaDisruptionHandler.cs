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

                // We need to find the DisruptionArea by ID (AreaId).
                // PrismaDirector has `get__disruptionAreas()` returning Dictionary<DisruptionAreaDefinition, DisruptionArea>.
                // Wait, Dictionary keys might be definitions.
                // We need to iterate to find the one with matching name.
                
                // Assuming we can access the private dictionary via reflection if needed, 
                // or if there is a public way.
                // The dump showed `get__disruptionAreas`. The name suggests it's likely internal/private backing field but exposed by unhollower?
                // Actually, often `get_` implies property.
                
                // Iterating the dictionary:
                var areas = director._disruptionAreas; // This might be the field name if property is `_disruptionAreas`.
                // Or maybe we have to use `director.GetDisruptionAreaDefinitions` and then lookup?
                
                // Let's try to access `director._disruptionAreas` if public, or iterate `director.GetDisruptionAreaDefinitions`?
                // `GetDisruptionAreaDefinitions` returns keys.
                // `SetDisruptionLevel` takes `DisruptionArea` object, NOT definition.
                
                // So we need to:
                // 1. Find Definition by name.
                // 2. Get DisruptionArea from Definition (using dictionary).
                // 3. Call SetDisruptionLevel.
                
                // If `_disruptionAreas` is accessible:
                // foreach (var kvp in director._disruptionAreas) { if (kvp.Key.name == packet.AreaId) ... }
                
                // Let's assume `_disruptionAreas` is exposed as a property or field.
                // If Il2CppInterop/MelonLoader exposes private fields, we might use that.
                // Otherwise, use reflection or `GetDisruptionAreaDefinitions` + lookup.
                
                // Using a safe approach:
                // We'll iterate definitions if possible.
                // Wait, `SceneContext`... `PrismaDirector`... 
                
                // Let's try:
                /*
                foreach (var kvp in director._disruptionAreas)
                {
                    if (kvp.Key.name == packet.AreaId)
                    {
                        director.SetDisruptionLevel(kvp.Value, (DisruptionLevel)packet.Level, packet.IsTransition);
                        break;
                    }
                }
                */
                // Note: Il2Cpp dictionaries are specific.
                
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
