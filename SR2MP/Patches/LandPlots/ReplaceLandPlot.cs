using HarmonyLib;
using SR2MP.Packets.LandPlots;

namespace SR2MP.Patches.LandPlots;

[HarmonyPatch(typeof(LandPlotLocation), nameof(LandPlotLocation.Replace))]
internal static class ReplaceLandPlot
{
    public static void Postfix(LandPlotLocation __instance, GameObject replacementPrefab)
    {
        if (HandlingPacket) return;

        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;

        var packet = new NewLandPlotPacket
        {
            PlotID = __instance._id,
            ID = replacementPrefab.GetComponent<LandPlot>().TypeId
        };

        Main.SendToAllOrServer(packet);
    }
}