using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Labyrinth;

[HarmonyPatch(typeof(PuzzleSlot), nameof(PuzzleSlot.ActivateOnFill))]
internal static class OnPuzzleSlotFill
{
    public static void Postfix(PuzzleSlot __instance)
    {
        if (HandlingPacket) return;
        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;

        NetworkPuzzleSlotManager.Broadcast(__instance, true);
    }
}

[HarmonyPatch(typeof(PuzzleSlot), nameof(PuzzleSlot.OnFilledChanged))]
internal static class OnPuzzleSlotFilledChanged
{
    public static void Postfix(PuzzleSlot __instance)
    {
        if (HandlingPacket) return;
        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;

        var model = __instance._model;
        if (model == null) return;

        NetworkPuzzleSlotManager.Broadcast(__instance, model.filled);
    }
}

[HarmonyPatch(typeof(PuzzleSlot), nameof(PuzzleSlot.Awake))]
internal static class OnPuzzleSlotAwake
{
    public static void Postfix(PuzzleSlot __instance)
        => NetworkPuzzleSlotManager.ApplyPendingState(__instance, __instance._model);
}

[HarmonyPatch(typeof(PuzzleSlot), nameof(PuzzleSlot.InitModel))]
internal static class OnPuzzleSlotModelInit
{
    public static void Postfix(PuzzleSlot __instance, PuzzleSlotModel model)
        => NetworkPuzzleSlotManager.ApplyPendingState(__instance, model);
}

[HarmonyPatch(typeof(PuzzleSlot), nameof(PuzzleSlot.SetModel))]
internal static class OnPuzzleSlotModelSet
{
    public static void Postfix(PuzzleSlot __instance, PuzzleSlotModel model)
        => NetworkPuzzleSlotManager.ApplyPendingState(__instance, model);
}
