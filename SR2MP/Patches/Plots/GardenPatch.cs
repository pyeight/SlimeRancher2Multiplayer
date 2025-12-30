using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Shared;
using SR2MP.Shared.Managers;
using UnityEngine;

namespace SR2MP.Patches.Plots
{
    [HarmonyPatch(typeof(GardenCatcher))]
    public static class GardenPatch
    {
        [HarmonyPatch(nameof(GardenCatcher.Plant))]
        [HarmonyPostfix]
        public static void PlantPostfix(GardenCatcher __instance, IdentifiableType cropId, bool isReplacement, GameObject __result)
        {
            if (GlobalVariables.handlingPacket) return;
            if (!Main.Server.IsRunning() && !Main.Client.IsConnected) return;
            if (__result == null) return; // Plant failed

            // Get the plot ID from the parent LandPlotLocation
            var plotLocation = __instance.GetComponentInParent<LandPlotLocation>();
            if (plotLocation == null) return;

            // Find which slot was planted
            // GardenCatcher has PlantSlot array, need to find the index
            // For now, we'll send slot 0 - may need refinement
            int slotIndex = 0;
            
            var packet = new GardenPlantPacket(
                plotLocation._id,
                GlobalVariables.actorManager.GetPersistentID(cropId),
                slotIndex
            );

            Main.SendToAllOrServer(packet);
        }
    }
}
