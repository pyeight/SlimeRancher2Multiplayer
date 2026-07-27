using HarmonyLib;
using SR2MP.Packets.GordoSlime;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.GordoSlime;

[HarmonyPatch(typeof(GordoEat), nameof(GordoEat.ImmediateReachedTarget))]
internal static class OnGordoSlimeBurst
{
    public static void Prefix(GordoEat __instance)
    {
        if (HandlingPacket) return;

        NetworkGordoSlimeManager.MarkPopped(__instance.Id);

        var packet = new GordoSlimeBurstPacket
        {
            ID = __instance.Id
        };
        Main.SendToAllOrServer(packet);
    }
}