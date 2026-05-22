using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Player;
using SR2MP.Packets.Ammo;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Ammo;

[HarmonyPatch(typeof(AmmoSlotManager), nameof(AmmoSlotManager.MaybeAddToAnySlot),
    typeof(AmmoSlot.AmmoMetadata),
    typeof(bool))]
internal static class OnAmmoAddToAny
{
    public static void Postfix(AmmoSlotManager __instance, ref bool __result, AmmoSlot.AmmoMetadata metadata)
    {
        if ((!Main.Client.IsConnected && !Main.Server.IsRunning) || HandlingPacket) return;

        if (!__result)
            return;

        var packet = new AmmoAddPacket()
        {
            Identifiable = NetworkActorManager.GetPersistentID(metadata.Id),
            Count = 1,
            ID = __instance.GetPlotID(),
        };

        if (packet.ID == null) return;

        Main.SendToAllOrServer(packet);
    }
}