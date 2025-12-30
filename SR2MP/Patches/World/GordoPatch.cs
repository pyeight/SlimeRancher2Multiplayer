using HarmonyLib;
using Il2Cpp;
using SR2MP.Packets.Shared;
using UnityEngine;

namespace SR2MP.Patches.World
{
    /*
    [HarmonyPatch(typeof(GordoEat), nameof(GordoEat.Eat))]
    public static class GordoPatch
    {
        public static void Postfix(GordoEat __instance)
        {
            if (GlobalVariables.handlingPacket) return;
            if (__instance == null || __instance.GordoModel == null) return;

            string name = __instance.gameObject.name;
            int count = __instance.GordoModel.GordoEatenCount;

            Main.SendToAllOrServer(new GordoEatPacket(name, count));
        }
    }
    */
}
