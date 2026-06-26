using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Labyrinth;
using SR2MP.Packets.World;

namespace SR2MP.Patches.Labyrinth;

[HarmonyPatch(typeof(PrismaBarrier), nameof(PrismaBarrier.SetActivationTime))]
internal static class OnPrismaBarrierActivate
{
    public static void Postfix(PrismaBarrier __instance, double activationTime)
    {
        if (HandlingPacket) return;

        var id = "";
        foreach (var pair in GameState.AllPrismaBarriers())
        {
            if (pair.value == __instance._model)
            {
                id = pair.key;
                break;
            }
        }

        if (!string.IsNullOrEmpty(id))
        {
            Main.SendToAllOrServer(new PrismaBarrierPacket
            {
                ID = id,
                ActivationTime = activationTime
            });
        }
    }
}