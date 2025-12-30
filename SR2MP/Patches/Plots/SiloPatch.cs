using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Shared;
using SR2MP.Shared.Managers;
using UnityEngine;

namespace SR2MP.Patches.Plots
{
    [HarmonyPatch(typeof(SiloStorage))]
    public static class SiloPatch
    {
        [HarmonyPatch(nameof(SiloStorage.MaybeAddIdentifiable), typeof(IdentifiableType), typeof(GameObject), typeof(int), typeof(int), typeof(bool))]
        [HarmonyPostfix]
        public static void MaybeAddPostfix(SiloStorage __instance, IdentifiableType id, GameObject inserted, int slotIdx, int count, bool overflow, bool __result)
        {
            if (GlobalVariables.handlingPacket) return;
            if (!Main.Server.IsRunning() && !Main.Client.IsConnected) return;
            if (!__result) return; // Add failed

            // Get the plot ID
            var plotLocation = __instance.GetComponentInParent<LandPlotLocation>();
            if (plotLocation == null) return;

            // Get current count in the slot
            int currentCount = __instance.GetSlotCount(slotIdx);

            var packet = new SiloUpdatePacket(
                plotLocation._id,
                slotIdx,
                GlobalVariables.actorManager.GetPersistentID(id),
                currentCount
            );

            Main.SendToAllOrServer(packet);
        }

        [HarmonyPatch(nameof(SiloStorage.OnIdentifiableRemoved))]
        [HarmonyPostfix]
        public static void OnRemovedPostfix(SiloStorage __instance, IdentifiableType id)
        {
            if (GlobalVariables.handlingPacket) return;
            if (!Main.Server.IsRunning() && !Main.Client.IsConnected) return;

            // Get the plot ID
            var plotLocation = __instance.GetComponentInParent<LandPlotLocation>();
            if (plotLocation == null) return;

            // Since we don't know which slot was removed from, send updates for all slots
            // This is less efficient but more reliable
            var ammo = __instance.GetRelevantAmmo();
            if (ammo == null) return;

            // Send update for first 4 slots (typical silo size)
            for (int i = 0; i < 4; i++)
            {
                var slotId = __instance.GetSlotIdentifiable(i);
                int currentCount = __instance.GetSlotCount(i);
                
                var packet = new SiloUpdatePacket(
                    plotLocation._id,
                    i,
                    slotId != null ? GlobalVariables.actorManager.GetPersistentID(slotId) : 0,
                    currentCount
                );

                Main.SendToAllOrServer(packet);
            }
        }
    }
}
