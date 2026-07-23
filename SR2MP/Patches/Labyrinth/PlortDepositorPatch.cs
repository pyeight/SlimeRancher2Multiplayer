using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Labyrinth;

[HarmonyPatch(typeof(PlortDepositor), nameof(PlortDepositor.OnFilledChangedFromModel))]
internal static class OnPlortDepositorDeposit
{
    public static void Postfix(PlortDepositor __instance)
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
                AmountDeposited = __instance._model.AmountDeposited
            });
        }
    }
}

[HarmonyPatch(typeof(PlortDepositor), nameof(PlortDepositor.Awake))]
internal static class OnPlortDepositorAwake
{
    public static void Postfix(PlortDepositor __instance)
        => NetworkDepositorManager.ApplyPendingState(__instance, __instance._model);
}

[HarmonyPatch(typeof(PlortDepositor), nameof(PlortDepositor.InitModel))]
internal static class OnPlortDepositorModelInit
{
    public static void Postfix(PlortDepositor __instance, PlortDepositorModel model)
        => NetworkDepositorManager.ApplyPendingState(__instance, model);
}

[HarmonyPatch(typeof(PlortDepositor), nameof(PlortDepositor.SetModel))]
internal static class OnPlortDepositorModelSet
{
    public static void Postfix(PlortDepositor __instance, PlortDepositorModel model)
        => NetworkDepositorManager.ApplyPendingState(__instance, model);
}
