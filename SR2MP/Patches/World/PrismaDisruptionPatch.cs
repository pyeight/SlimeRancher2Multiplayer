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
                // Reverse lookup the area ID from the dictionary

                string areaId = "";
                
                // Reverse lookup in _disruptionAreas
                var dict = __instance._disruptionAreas;
                if (dict != null)
                {
                    foreach (var entry in dict)
                    {
                        if (entry.Value.Equals(area))
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
