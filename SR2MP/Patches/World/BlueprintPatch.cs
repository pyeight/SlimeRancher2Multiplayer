using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Shared;
using UnityEngine;

namespace SR2MP.Patches.World
{
    [HarmonyPatch(typeof(GadgetDirector), nameof(GadgetDirector.AddBlueprint))]
    public static class BlueprintPatch
    {
        public static void Postfix(GadgetDirector __instance, Il2Cpp.GadgetDefinition gadgetDefinition)
        {
            if (GlobalVariables.handlingPacket) return;

            string id = gadgetDefinition.name; // or gadgetDefinition.Id
            
            Main.SendToAllOrServer(new BlueprintUnlockPacket(id));
        }
    }
}
