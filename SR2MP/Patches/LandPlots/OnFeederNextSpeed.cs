using HarmonyLib;
using SR2MP.Packets.LandPlots;

namespace SR2MP.Patches.LandPlots;

[HarmonyPatch(typeof(SlimeFeeder), nameof(SlimeFeeder.StepFeederSpeed))]
internal static class OnFeederNextSpeed
{
    public static void Postfix(SlimeFeeder __instance)
    {
        if (HandlingPacket) return;

        var packet = new AutoFeederSpeedPacket()
        {
            Speed = __instance._model.feederCycleSpeed,
            ID = __instance.gameObject.GetComponentInParent<LandPlotLocation>()._id
        };

        Main.SendToAllOrServer(packet);
    }
}