using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Weather;
using Il2CppMonomiPark.SlimeRancher.World;
using SR2MP.Server.Managers;

namespace SR2MP.Patches.Weather;

// Weather Registry
[HarmonyPatch(typeof(WeatherRegistry))]
public static class WeatherReigstryPatches
{
    [HarmonyPatch(nameof(WeatherRegistry.Update)), HarmonyPrefix]
    public static bool UpdatePrefix() => !Main.Client.IsConnected;

    [HarmonyPatch(nameof(WeatherRegistry.RunPatternState)), HarmonyPrefix]
    public static bool RunPatternStatePrefix()
    {
        WeatherUpdateHelper.EnsureLookupInitialized();
        return !Main.Client.IsConnected || handlingPacket;
    }

    [HarmonyPatch(nameof(WeatherRegistry.StopPatternState)), HarmonyPrefix]
    public static bool StopPatternStatePrefix(WeatherRegistry __instance, ZoneDefinition zone)
    {
        WeatherUpdateHelper.EnsureLookupInitialized();

        if (!zone)
            return false;

        return !Main.Client.IsConnected || handlingPacket;
    }
}

// Weather Director