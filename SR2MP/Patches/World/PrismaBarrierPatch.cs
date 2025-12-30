using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Shared;

namespace SR2MP.Patches.World
{
    [HarmonyPatch(typeof(PrismaBarrierModel))]
    public static class PrismaBarrierPatch
    {
        [HarmonyPatch(nameof(PrismaBarrierModel.ActivationTime), MethodType.Setter)]
        [HarmonyPostfix]
        public static void SetActivationTimePostfix(PrismaBarrierModel __instance, double value)
        {
            if (GlobalVariables.handlingPacket) return;

            if (Main.Client.IsConnected || Main.Server.IsRunning())
            {
                var packet = new PrismaBarrierPacket(
                    __instance.Id,
                    value
                );

                Main.SendToAllOrServer(packet);
            }
        }
    }
}
