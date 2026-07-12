using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Weather;

namespace SR2MP.Patches.Weather;

[HarmonyPatch(typeof(WeatherRegistry), nameof(WeatherRegistry.Update))]
internal static class OnWeatherRegistryUpdate
{
    [HarmonyFinalizer]
    public static Exception? Finalizer(Exception? __exception)
    {
        if (__exception == null)
            return null;

        SrLogger.LogDebug("Suppressed exception: WeatherRegistry.Update");
        return null;
    }
}
