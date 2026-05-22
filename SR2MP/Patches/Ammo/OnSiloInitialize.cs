using System.Collections;
using HarmonyLib;
using MelonLoader;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Ammo;

[HarmonyPatch(typeof(SiloStorage), nameof(SiloStorage.InitAmmo))]
internal static class OnSiloInitialize
{
    public static void Postfix(SiloStorage __instance)
    {
        StartCoroutine(Initialize(__instance));
    }

    private static IEnumerator Initialize(SiloStorage siloStorage)
    {
        yield return new WaitFrames(3);

        try
        {
            siloStorage.RegisterAmmoPointer();
        }
        catch (Exception e)
        {
            SrLogger.LogError($"Error on Silo Initialization!\n{e}\n");
        }
    }
}