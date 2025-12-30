using HarmonyLib;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.System;

[HarmonyPatch(typeof(GameContext), nameof(GameContext.Start))]
public static class GameContextPatch
{
    public static void Postfix(GameContext __instance)
    {
        GlobalVariables.actorManager.Initialize(__instance);
    }
}
