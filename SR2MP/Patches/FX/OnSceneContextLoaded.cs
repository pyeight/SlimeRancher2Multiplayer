using System.Collections;
using HarmonyLib;
using MelonLoader;

namespace SR2MP.Patches.FX;

[HarmonyPatch(typeof(SceneContext), nameof(SceneContext.Start))]
public static class OnSceneContextLoaded
{
    private static IEnumerator WaitForFinishLoading()
    {
        while (SystemContext.Instance.SceneLoader.IsSceneLoadInProgress)
        {
            yield return null;
        }

        fxManager.Initialize();
    }
    public static void Postfix(SceneContext __instance)
    {
        MelonCoroutines.Start(WaitForFinishLoading());
    }
}