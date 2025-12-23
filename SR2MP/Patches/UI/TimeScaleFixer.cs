using HarmonyLib;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.Script.UI.Pause;

namespace SR2MP.Patches.UI;

[HarmonyPatch(typeof(PauseMenuDirector), nameof(PauseMenuDirector.PauseGame))]
public static class TimeScaleFixer
{
    public static bool Prefix(PauseMenuDirector __instance)
    {
        return !GameContext.Instance.InputDirector._paused.Map.enabled;
    }
}