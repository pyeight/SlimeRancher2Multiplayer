using HarmonyLib;
using Il2Cpp;
using SR2MP.Packets.Shared;
using UnityEngine;

namespace SR2MP.Patches.World
{
    [HarmonyPatch(typeof(TreasurePod), nameof(TreasurePod.Activate))]
    public static class TreasurePodPatch
    {
        public static void Postfix(TreasurePod __instance)
        {
            if (GlobalVariables.handlingPacket) return;

            string name = __instance.gameObject.name;

            Main.SendToAllOrServer(new TreasurePodPacket(name));
        }
    }
}
