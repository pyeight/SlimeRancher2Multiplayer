using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Slime.Dervish;
using SR2MP.Components.Actor;

namespace SR2MP.Patches.Slime.Dervish;

[HarmonyPatch(typeof(DervishSlimeSpin), nameof(DervishSlimeSpin.SpawnCyclone))]
internal static class DervishSlimeSpinSpawnCyclone
{
    public static bool Prefix(DervishSlimeSpin __instance)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return true;
        if (HandlingPacket) return true;

        var networkActor = __instance.GetComponent<NetworkActor>();
        if (networkActor.LocallyOwned) return true;
        
        return false;
    }
}