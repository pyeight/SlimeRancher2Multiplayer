using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.World;

namespace SR2MP.Patches.System;

[HarmonyPatch(typeof(GameContext), nameof(GameContext.Start))]
public static class ClassDumperPatch
{
    private static bool dumped = false;

    public static void Postfix()
    {
        if (dumped) return;
        dumped = true;

        try
        {
            var dumpFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SR2MP_ClassDump.txt");
            SrLogger.LogMessage($"Dumping classes to {dumpFile}...");

            using var writer = new StreamWriter(dumpFile);
            
            // Get all assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (var assembly in assemblies)
            {
                writer.WriteLine($"Assembly: {assembly.FullName}");
                try
                {
                    // Filter for interesting assemblies
                    if (!assembly.FullName.Contains("Il2Cpp") && !assembly.FullName.Contains("Assembly-CSharp"))
                        continue;

                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        writer.WriteLine($"  Type: {type.FullName}");
                    }
                }
                catch (Exception e)
                {
                    writer.WriteLine($"  Error reading assembly: {e.Message}");
                }
                writer.WriteLine();
            }

            SrLogger.LogMessage("Class dump complete!");
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to dump classes: {ex}");
        }
    }
}
