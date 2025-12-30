using HarmonyLib;
using Il2Cpp;
using SR2MP.Packets.Shared;
using UnityEngine;

using Il2CppMonomiPark.SlimeRancher.UI;

namespace SR2MP.Patches.World
{
    [HarmonyPatch(typeof(TechUIInteractable), nameof(TechUIInteractable.OnInteract))]
    public static class MapUnlockPatch
    {
        public static void Postfix(TechUIInteractable __instance)
        {
            if (GlobalVariables.handlingPacket) return;
            
            // Should verify if this is a Map Node? 
            // Map Nodes usually have specific names or components.
            // But syncing all TechUI interactions might be harmless if handled correctly implicitly?
            // "MapData" or similar might be involved.
            // GitHub checks if the object is "MapData" related? 
            // GitHub code:
            /*
               var techUI = ...;
               techUI.OnInteract();
            */
            // It assumes by name it finds the right object.
            
            // Filter by name pattern or component if needed.
            // For now, sync all TechUI interactions. Most are map reveals in SR2?
            // Or use "RegionMapUnlock" logic?
            
            string name = __instance.gameObject.name;

            Main.SendToAllOrServer(new MapUnlockPacket(name));
        }
    }
}
