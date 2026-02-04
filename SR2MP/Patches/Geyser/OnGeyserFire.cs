using HarmonyLib;
using SR2MP.Packets.Geyser;

namespace SR2MP.Patches.Geyser;

[HarmonyPatch(typeof(Il2Cpp.Geyser._RunGeyser_d__30), nameof(Il2Cpp.Geyser._RunGeyser_d__30.MoveNext))]
public static class OnGeyserFire
{
    public static void Prefix(Il2Cpp.Geyser._RunGeyser_d__30 __instance)
    {
        if (__instance.__1__state != 0)
            return;

        if (handlingPacket)
            return;
        
        var packet = new GeyserTriggerPacket
        {
             ObjectPath = __instance.__4__this.gameObject.GetGameObjectPath(),
             Duration = __instance.duration
        };

        Main.SendToAllOrServer(packet);
    }
}