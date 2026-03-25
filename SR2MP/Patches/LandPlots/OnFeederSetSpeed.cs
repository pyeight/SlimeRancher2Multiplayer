using HarmonyLib;
using SR2MP.Packets.LandPlots;

namespace SR2MP.Patches.LandPlots;

[HarmonyPatch(typeof(SlimeFeeder), nameof(SlimeFeeder.SetFeederSpeed))]
public static class OnFeederSetSpeed
{
    public static void Postfix(SlimeFeeder __instance, SlimeFeeder.FeedSpeed speed)
    {
        if (HandlingPacket) return;

        var packet = new AutoFeederSpeedPacket()
        {
            Speed = speed,
            ID = __instance.gameObject.GetComponentInParent<LandPlotLocation>()._id
        };

        Main.SendToAllOrServer(packet);
    }
}