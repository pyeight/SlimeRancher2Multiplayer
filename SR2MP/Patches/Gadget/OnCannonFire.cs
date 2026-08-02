using HarmonyLib;
using SR2MP.Packets.Gadget;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Gadget;

[HarmonyPatch(typeof(LinkedCannonInput), nameof(LinkedCannonInput.Update))]
internal static class OnCannonFire
{
    public static void Postfix(LinkedCannonInput __instance)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;
        if (HandlingPacket) return;

        try
        {
            if (SystemContext.Instance.SceneLoader.IsSceneLoadInProgress) return;

            var gadget = __instance._gadget;
            if (!gadget) return;

            var gadgetId = gadget.GetActorId().Value;

            if (!NetworkGadgetManager.TryRecordCannonFireTime(gadgetId, __instance._nextFireTime))
                return;

            Main.SendToAllOrServer(new CannonFirePacket
            {
                GadgetId = gadgetId,
                NextFireTime = __instance._nextFireTime
            });
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"OnCannonFire: failed to broadcast cannon timer: {ex.Message}");
        }
    }
}
