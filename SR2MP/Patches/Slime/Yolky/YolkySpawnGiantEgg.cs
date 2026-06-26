using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Slime;
using SR2MP.Components.Actor;

namespace SR2MP.Patches.Slime.Yolky;

[HarmonyPatch(typeof(SpawnGiantEgg), nameof(SpawnGiantEgg.CreateGiantEgg))]
internal static class YolkySpawnGiantEgg
{
    public static bool Prefix(SpawnGiantEgg __instance, ref GameObject __result)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return true;
        if (HandlingPacket) return true;

        var networkActor = __instance.GetComponent<NetworkActor>();
        if (networkActor != null && !networkActor.LocallyOwned)
        {
            __result = null!;
            return false;
        }

        return true;
    }
}