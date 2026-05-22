using HarmonyLib;
using SR2MP.Packets.GordoSlime;

namespace SR2MP.Patches.GordoSlime;

[HarmonyPatch(typeof(GordoEat), nameof(GordoEat.ImmediateReachedTarget))]
internal static class OnGordoBurst
{
    public static void Prefix(GordoEat __instance)
    {
        if (HandlingPacket) return;

        var packet = new GordoSlimeBurstPacket
        {
            ID = __instance.Id
        };
        Main.SendToAllOrServer(packet);
    }
}