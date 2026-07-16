using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Labyrinth;

[HarmonyPatch(typeof(PuzzleSlot), nameof(PuzzleSlot.ActivateOnFill))]
internal static class OnPuzzleSlotFill
{
    public static void Postfix(PuzzleSlot __instance)
    {
        if (HandlingPacket) return;
        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;

        var id = NetworkPuzzleSlotManager.ResolveId(__instance, __instance._model);
        if (string.IsNullOrEmpty(id))
            return;

        Main.SendToAllOrServer(new PuzzleSlotPacket
        {
            ID = id!,
            Filled = true
        });
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
