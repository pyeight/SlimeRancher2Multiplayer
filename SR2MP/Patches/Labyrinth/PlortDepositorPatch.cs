using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Labyrinth;

[HarmonyPatch(typeof(PlortDepositor), nameof(PlortDepositor.OnFilledChangedFromModel))]
internal static class OnPlortDepositorDeposit
{
    public static void Postfix(PlortDepositor __instance)
    {
        if (HandlingPacket) return;

        NetworkPlortDepositorManager.Broadcast(__instance);
    }
}

[HarmonyPatch(typeof(PlortDepositor), nameof(PlortDepositor.OnFilledChanged))]
internal static class OnPlortDepositorFilledChanged
{
    public static void Postfix(PlortDepositor __instance)
    {
        if (HandlingPacket) return;

        NetworkPlortDepositorManager.Broadcast(__instance);
    }
}

[HarmonyPatch(typeof(PlortDepositor), nameof(PlortDepositor.Awake))]
internal static class OnPlortDepositorAwake
{
    public static void Postfix(PlortDepositor __instance)
        => NetworkPlortDepositorManager.ApplyPendingState(__instance, __instance._model);
}

[HarmonyPatch(typeof(PlortDepositor), nameof(PlortDepositor.InitModel))]
internal static class OnPlortDepositorModelInit
{
    public static void Postfix(PlortDepositor __instance, PlortDepositorModel model)
        => NetworkPlortDepositorManager.ApplyPendingState(__instance, model);
}

[HarmonyPatch(typeof(PlortDepositor), nameof(PlortDepositor.SetModel))]
internal static class OnPlortDepositorModelSet
{
    public static void Postfix(PlortDepositor __instance, PlortDepositorModel model)
        => NetworkPlortDepositorManager.ApplyPendingState(__instance, model);
}
