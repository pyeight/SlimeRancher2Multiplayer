using HarmonyLib;
using SR2MP.Components.Actor;

namespace SR2MP.Patches.Slime.Lucky;

[HarmonyPatch(typeof(Il2Cpp.LuckySlimeProduceCoins), nameof(Il2Cpp.LuckySlimeProduceCoins.ProduceCoins))]
internal static class LuckySlimeProduceCoins
{
    public static bool Prefix(Il2Cpp.LuckySlimeProduceCoins __instance)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return true;
        if (HandlingPacket) return true;

        var networkActor = __instance.GetComponent<NetworkActor>();
        if (networkActor != null && !networkActor.LocallyOwned)
            return false;

        return true;
    }
}