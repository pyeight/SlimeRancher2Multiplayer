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

        var id = "";
        foreach (var pair in GameState.slots)
        {
            if (pair.value == __instance._model)
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