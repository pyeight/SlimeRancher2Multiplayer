using HarmonyLib;
using System.Collections;
using MelonLoader;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Ammo;

[HarmonyPatch(typeof(SiloStorage), nameof(SiloStorage.InitAmmo))]
public static class OnSiloInitialize
{
    public static void Postfix(SiloStorage __instance)
    {
        MelonCoroutines.Start(Initialize(__instance));
    }

    public static IEnumerator Initialize(SiloStorage siloStorage)
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