using HarmonyLib;
using SR2MP.Packets.LandPlots;

namespace SR2MP.Patches.Plots;

[HarmonyPatch(typeof(LandPlotLocation), nameof(LandPlotLocation.Replace))]
public static class ReplaceLandPlot
{
    public static void Postfix(LandPlotLocation __instance, GameObject replacementPrefab)
    {
        if (handlingPacket) return;

        if (!Main.Server.IsRunning() && !Main.Client.IsConnected) return;

        var packet = new NewLandPlotPacket
        {
            ID = __instance._id,
            PlotType = replacementPrefab.GetComponent<LandPlot>().TypeId
        };

        Main.SendToAllOrServer(packet);
    }
}