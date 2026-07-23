using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Dialogue.CommStation;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Events;

[HarmonyPatch(typeof(FixedConversation), nameof(FixedConversation.RecordPlayed))]
internal static class OnConversationPlayed
{
    public static void Prefix(FixedConversation __instance, out bool __state)
        => __state = !__instance.HasBeenPlayed();

    public static void Postfix(FixedConversation __instance, bool __state)
    {
        if (HandlingPacket) return;
        if (!__state) return;

        NetworkConversationManager.SendConversationPlayed(__instance.name);
    }
}
