using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Labyrinth;
using SR2MP.Packets.Shared;

namespace SR2MP.Patches.World
{
    [HarmonyPatch(typeof(PrismaDirector))]
    public static class PrismaDisruptionPatch
    {
        [HarmonyPatch(nameof(PrismaDirector.SetDisruptionLevel))]
        [HarmonyPostfix]
        public static void SetDisruptionLevelPostfix(PrismaDirector __instance, PrismaDirector.DisruptionArea area, DisruptionLevel level, bool isTransition)
        {
            if (GlobalVariables.handlingPacket) return;

            if (Main.Client.IsConnected || Main.Server.IsRunning())
            {
                // We need to find the ID (name) of the area.
                // DisruptionArea doesn't have a name property directly visible in quick look, 
                // but it likely has a reference to its definition.
                // Or we can reverse lookup in the dictionary.
                
                string areaId = "";
                
                // Reverse lookup in _disruptionAreas
                var dict = __instance._disruptionAreas;
                if (dict != null)
                {
                    foreach (var entry in dict)
                    {
                        if (entry.Value.Equals(area)) // Assuming value equality or ref check
                        {
                            areaId = entry.Key.name;
                            break;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(areaId))
                {
                    var packet = new PrismaDisruptionPacket(
                        areaId,
                        (int)level,
                        isTransition
                    );

                    Main.SendToAllOrServer(packet);
                }
                else
                {
                    MelonLoader.MelonLogger.Warning("Could not find ID for DisruptionArea in SetDisruptionLevel patch.");
                }
            }
        }
    }
}
