using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Player;
using SR2MP.Packets.Ammo;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Ammo;

[HarmonyPatch(typeof(AmmoSlotManager), nameof(AmmoSlotManager.MaybeAddToSlot), typeof(IdentifiableType),
    typeof(Identifiable),
    typeof(SlimeAppearance.AppearanceSaveSet),
    typeof(bool))]
internal static class OnAmmoAddToAny
{
    public static void Postfix(AmmoSlotManager __instance, ref bool __result, IdentifiableType id)
    {
        if ((!Main.Client.IsConnected && !Main.Server.IsRunning) || HandlingPacket) return;

        if (!__result)
            return;

        var packet = new AmmoAddPacket()
        {
            Identifiable = NetworkActorManager.GetPersistentID(id),
            Count = 1,
            ID = __instance.GetPlotID(),
        };

        if (packet.ID == null) return;

        Main.SendToAllOrServer(packet);
    }
}