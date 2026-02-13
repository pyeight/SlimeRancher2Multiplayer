using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.World;
using SR2MP.Packets.World;

namespace SR2MP.Patches.Weather;

[HarmonyPatch(typeof(LightningStrike), nameof(LightningStrike.Start))]
public static class OnLightningActivate
{
    public static void Postfix(LightningStrike __instance)
    {
        if (__instance.gameObject.name.Contains("net", StringComparison.InvariantCultureIgnoreCase))
            return;

        var packet = new LightningStrikePacket { Position = __instance.transform.position };
        Main.SendToAllOrServer(packet);
    }
}