using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Player;
using SR2MP.Packets.LandPlots;

namespace SR2MP.Patches.LandPlots;

[HarmonyPatch(typeof(SlimeFeeder), nameof(SlimeFeeder.EjectFood))]
internal static class OnFeederEject
{
    public static void Postfix(SlimeFeeder __instance, AmmoSlotManager storageAmmo)
    {
        if (HandlingPacket) return;

        var packet = new AutoFeederDispensePacket()
        {
            ID = __instance.gameObject.GetComponentInParent<LandPlotLocation>()._id,
            NextTime = __instance._nextEject,
        };

        Main.SendToAllOrServer(packet);
    }
}