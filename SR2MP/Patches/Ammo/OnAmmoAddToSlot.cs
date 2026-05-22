using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Player;
using SR2MP.Packets.Ammo;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Ammo
{
    [HarmonyPatch(typeof(AmmoSlotManager), nameof(AmmoSlotManager.MaybeAddToSpecificSlot),
        typeof(AmmoSlot.AmmoMetadata), typeof(int), typeof(int), typeof(bool))]
    internal static class OnAmmoAddToSlot
    {
        public static void Postfix(AmmoSlotManager __instance, ref bool __result, AmmoSlot.AmmoMetadata metadata, int slotIdx, int count)
        {
            if ((!Main.Client.IsConnected && !Main.Server.IsRunning) || HandlingPacket) return;

            if (!__result)
                return;

            var packet = new AmmoAddToSlotPacket()
            {
                Identifiable = NetworkActorManager.GetPersistentID(metadata.Id),
                SlotIndex = slotIdx,
                Count = count,
                ID = __instance.GetPlotID()
            };

            if (packet.ID == null) return;

            Main.SendToAllOrServer(packet);
        }
    }
}