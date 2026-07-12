using HarmonyLib;
using SR2MP.Components.Actor;

namespace SR2MP.Patches.Slime.Yolky;

[HarmonyPatch(typeof(GiantEggBreakOnImpact), nameof(GiantEggBreakOnImpact.BreakOpen))]
internal static class YolkyGiantEggBreak
{
    public static bool Prefix(GiantEggBreakOnImpact __instance)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return true;
        if (HandlingPacket) return true;

        var networkActor = __instance.GetComponent<NetworkActor>();
        if (networkActor != null && !networkActor.LocallyOwned)
            return false;

        return true;
    }
}