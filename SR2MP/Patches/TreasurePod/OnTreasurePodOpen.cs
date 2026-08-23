using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.TreasurePod;

[HarmonyPatch(typeof(Il2Cpp.TreasurePod), nameof(Il2Cpp.TreasurePod.Activate))]
internal static class OnTreasurePodOpen
{
    public static void Postfix(Il2Cpp.TreasurePod __instance)
    {
        if (HandlingPacket) return;

        NetworkTreasurePodManager.Broadcast(__instance.Id, Il2Cpp.TreasurePod.State.OPEN);
    }
}

[HarmonyPatch(typeof(TreasurePodRewarder), nameof(TreasurePodRewarder.Activate))]
internal static class OnTreasurePodRewarderOpen
{
    public static void Postfix(TreasurePodRewarder __instance)
    {
        if (HandlingPacket) return;

        NetworkTreasurePodManager.Broadcast(__instance.Id, Il2Cpp.TreasurePod.State.OPEN);
    }
}

[HarmonyPatch(typeof(Il2Cpp.TreasurePod), nameof(Il2Cpp.TreasurePod.SetModel))]
internal static class OnTreasurePodModelSet
{
    public static void Postfix(Il2Cpp.TreasurePod __instance, TreasurePodModel model)
        => NetworkTreasurePodManager.ApplyPendingState(__instance.Id, model);
}

[HarmonyPatch(typeof(TreasurePodRewarder), nameof(TreasurePodRewarder.SetModel))]
internal static class OnTreasurePodRewarderModelSet
{
    public static void Postfix(TreasurePodRewarder __instance, TreasurePodModel model)
        => NetworkTreasurePodManager.ApplyPendingState(__instance.Id, model);
}