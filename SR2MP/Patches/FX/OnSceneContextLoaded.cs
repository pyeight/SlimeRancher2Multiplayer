using System.Collections;
using HarmonyLib;
using MelonLoader;

namespace SR2MP.Patches.FX;

[HarmonyPatch(typeof(SceneContext), nameof(SceneContext.Start))]
internal static class OnSceneContextLoaded
{
    private static IEnumerator WaitForFinishLoading()
    {
        yield return new WaitForSceneGroupLoad();

        FXManager.Initialize();
    }

    public static void Postfix()
    {
        StartCoroutine(WaitForFinishLoading());
    }
}