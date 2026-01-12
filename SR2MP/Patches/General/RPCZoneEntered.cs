using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.General;

[HarmonyPatch(typeof(PlayerZoneTracker), nameof(PlayerZoneTracker.OnEntered))]
public static class RPCZoneEntered
{
    public static void Prefix(ZoneDefinition zone)
    {
        DiscordRPCManager.currentZone = zone;
        DiscordRPCManager.UpdatePresence();
    }
}