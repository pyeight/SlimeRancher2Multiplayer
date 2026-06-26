using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.World;
using SR2MP.Packets.World;

namespace SR2MP.Patches.Weather;

[HarmonyPatch]
internal static class OnLightningActivate
{
    [HarmonyPatch(typeof(LightningStrike), nameof(LightningStrike.Start)), HarmonyPostfix]
    public static void StartPostfix(LightningStrike __instance)
    {
        if (__instance.gameObject.name.Contains("(net)"))
            return;

        var packet = new LightningStrikePacket { Position = __instance.transform.position };
        Main.SendToAllOrServer(packet);
    }

    [HarmonyPatch(typeof(LightningStrike), nameof(LightningStrike.Awake)), HarmonyPostfix]
    public static void AwakePostfix(LightningStrike __instance)
    {
        if (Main.Client.IsConnected && !Main.Server.IsRunning)
        {
            __instance.SpawnOptions?.Clear();
        }
    }
}