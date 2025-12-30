using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Shared;
using UnityEngine;

namespace SR2MP.Patches.World
{
    [HarmonyPatch(typeof(DecorizerModel))]
    public static class DecorizerPatch
    {
        // DecorizerModel.Add takes only IdentifiableType - it's an inventory count system
        // DecorizerModel.Remove takes only IdentifiableType
        
        [HarmonyPatch(nameof(DecorizerModel.Add), typeof(IdentifiableType))]
        [HarmonyPostfix]
        public static void AddPostfix(DecorizerModel __instance, IdentifiableType id, bool __result)
        {
            if (GlobalVariables.handlingPacket) return;
            if (!__result) return; // if add failed, don't sync

            if (Main.Client.IsConnected || Main.Server.IsRunning())
            {
                var packet = new DecorationPacket(
                    id.GetHashCode(), 
                    id,
                    Vector3.zero, // DecorizerModel doesn't track position
                    Quaternion.identity,
                    false
                );

                Main.SendToAllOrServer(packet);
            }
        }

        [HarmonyPatch(nameof(DecorizerModel.Remove), typeof(IdentifiableType))]
        [HarmonyPostfix]
        public static void RemovePostfix(DecorizerModel __instance, IdentifiableType id, bool __result)
        {
            if (GlobalVariables.handlingPacket) return;
            if (!__result) return;

            if (Main.Client.IsConnected || Main.Server.IsRunning())
            {
                var packet = new DecorationPacket(
                    id.GetHashCode(),
                    id,
                    Vector3.zero,
                    Quaternion.identity,
                    true
                );

                Main.SendToAllOrServer(packet);
            }
        }
    }
}
