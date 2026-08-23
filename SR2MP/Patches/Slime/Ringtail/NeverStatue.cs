using HarmonyLib;

namespace SR2MP.Patches.Slime.Ringtail;

// This is a temporary fix to solve the desync-issue on Ringtail slimes

[HarmonyPatch(typeof(TurnToStoneInDaylight), nameof(TurnToStoneInDaylight.BecomeStatue))]
public static class NeverStatue
{
    public static bool Prefix(TurnToStoneInDaylight __instance)
    {
        if (!Main.Client.IsConnected && !Main.Server.IsRunning) return true;
        
        if (Main.AllowRingtail) return true;
        
        return false;
    }
}