using HarmonyLib;
using SR2MP.Packets.LandPlots;

namespace SR2MP.Patches.LandPlots;

[HarmonyPatch(typeof(LandPlot), nameof(LandPlot.AddUpgrade))]
internal static class UpgradeLandPlot
{
    public static void Postfix(LandPlot __instance, LandPlot.Upgrade upgrade)
    {
        if (HandlingPacket) return;

        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;

        if (!__instance)
            return;

        var packet = new LandPlotUpgradePacket
        {
            PlotID = __instance.GetComponentInParent<LandPlotLocation>()._id,
            ID = upgrade
        };

        Main.SendToAllOrServer(packet);
    }
}