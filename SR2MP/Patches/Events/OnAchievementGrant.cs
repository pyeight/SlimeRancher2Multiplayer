using HarmonyLib;

namespace SR2MP.Patches.Events;

[HarmonyPatch(typeof(MetaGameDirector))]
internal static class OnAchievementGrant
{
    [HarmonyPatch(nameof(MetaGameDirector.Grant), typeof(MetaGameDirector.Achievement), typeof(int))]
    [HarmonyPrefix]
    private static bool GrantByIdPrefix() => !Main.DisableAchievements;

    [HarmonyPatch(nameof(MetaGameDirector.Grant), typeof(MetaGameDirector.AchievementData), typeof(int))]
    [HarmonyPrefix]
    private static bool GrantByDataPrefix() => !Main.DisableAchievements;
}
