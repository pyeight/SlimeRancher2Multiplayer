using System.Collections;
using HarmonyLib;
using MelonLoader;

namespace SR2MP.Patches.FX;

[HarmonyPatch(typeof(SceneContext), nameof(SceneContext.Start))]
public static class OnSceneContextLoaded
{
    private static IEnumerator WaitForFinishLoading()
    {
        yield return new WaitForSceneGroupLoad();

        fxManager.Initialize();
    }

    public static void Postfix()
    {
        MelonCoroutines.Start(WaitForFinishLoading());
    }
}