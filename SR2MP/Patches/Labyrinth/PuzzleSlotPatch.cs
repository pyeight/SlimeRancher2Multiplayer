using HarmonyLib;
using Il2Cpp;
using SR2MP.Packets.World;

namespace SR2MP.Patches.Labyrinth;

[HarmonyPatch(typeof(PuzzleSlot), nameof(PuzzleSlot.OnFilledChanged))]
internal static class OnPuzzleSlotFill
{
    public static void Postfix(PuzzleSlot __instance)
    {
        if (HandlingPacket) return;

        if (__instance._model == null)
            return;

        var id = "";
        foreach (var pair in GameState.slots)
        {
            if (pair.value != null && pair.value.Pointer == __instance._model.Pointer)
            {
                id = pair.key;
                break;
            }
        }

        if (!string.IsNullOrEmpty(id))
        {
            Main.SendToAllOrServer(new PuzzleSlotPacket
            {
                ID = id,
                Filled = __instance._model.filled
            });
        }
    }
}