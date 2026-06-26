using HarmonyLib;
using SR2MP.Packets.World;

namespace SR2MP.Patches.Labyrinth;

[HarmonyPatch(typeof(PlortDepositor), nameof(PlortDepositor.OnFilledChanged))]
internal static class OnPlortDepositorDeposit
{
    public static void Postfix(PlortDepositor __instance, bool isInstant)
    {
        if (HandlingPacket) return;

        var id = "";
        foreach (var pair in GameState.depositors)
        {
            if (pair.value == __instance._model)
            {
                id = pair.key;
                break;
            }
        }

        if (!string.IsNullOrEmpty(id))
        {
            Main.SendToAllOrServer(new PlortDepositorPacket
            {
                ID = id,
                AmountDeposited = __instance._model.AmountDeposited,
                IsInstant = isInstant
            });
        }
    }
}