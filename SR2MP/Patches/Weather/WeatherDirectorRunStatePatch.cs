using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Weather;
using SR2MP.Server.Managers;

namespace SR2MP.Patches.Weather;

[HarmonyPatch(typeof(WeatherDirector), nameof(WeatherDirector.RunState))]
public static class WeatherDirectorRunStatePatch
{
    public static bool Prefix()
    {
        WeatherUpdateHelper.EnsureLookupInitialized();
        return !Main.Client.IsConnected || handlingPacket;
    }

    public static void Postfix()
    {
        if (Main.Server.IsRunning() && !handlingPacket)
        {
            WeatherUpdateHelper.SendWeatherUpdate();
        }
    }
}